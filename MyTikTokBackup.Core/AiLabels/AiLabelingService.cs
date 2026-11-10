using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML.OnnxRuntime;
using MyTikTokBackup.Core.Database;
using MyTikTokBackup.Core.Messages;
using MyTikTokBackup.Core.Services;
using Serilog;

namespace MyTikTokBackup.Core.AiLabels
{
    public enum AiRunKind
    {
        /// <summary>Only recompute labels from cached embeddings (after category assignments changed).</summary>
        RescoreOnly = 0,
        /// <summary>Embed new or changed videos, then recompute labels.</summary>
        Incremental = 1,
    }

    public interface IAiLabelingService
    {
        AiLabelingStatus Status { get; }

        /// <summary>Registers for app messages and schedules the startup run when the feature is enabled.</summary>
        void Start();
        void Enable();
        void Disable();
        /// <summary>Turns the slow analysis of video frames on or off; when off, only descriptions are used.</summary>
        void SetAnalyzeVideoFrames(bool value);
        void RequestRun(AiRunKind kind, TimeSpan delay);
        /// <summary>Deletes all suggested labels and runs the labeling again.</summary>
        Task RecomputeAllAsync();
        void Cancel();
    }

    /// <summary>
    /// Background process that assigns AI labels (suggested user categories) to videos.
    /// One worker processes debounced run requests; see <see cref="AiLabelScorer"/> for the scoring.
    /// </summary>
    public class AiLabelingService : IAiLabelingService
    {
        private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan DownloadDelay = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan CategoriesDelay = TimeSpan.FromSeconds(3);

        private const int TextBatchSize = 64;
        private const int FramesPerVideo = 4;
        private const int ImageSaveBatchSize = 20;
        private const int RescoreEveryImages = 200;
        private const string FailedPrefix = "failed|";

        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase) { ".mp4", ".mov", ".webm", ".mkv" };
        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpeg", ".jpg", ".png", ".webp" };

        private readonly IAppConfiguration _appConfiguration;
        private readonly AiModelManager _modelManager;
        private readonly ILocalVideosService _localVideosService;
        private readonly IMediaFrameExtractor _frameExtractor;
        private readonly AiLabelingOptions _options = AiLabelingOptions.Default;

        private readonly object _lock = new object();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private AiRunKind? _pending;
        private DateTime _dueAt;
        private CancellationTokenSource _runCts;
        private Task _worker;

        // Accessed only by the worker.
        private readonly Dictionary<string, float[]> _nameVectors = new();

        private AiLabelingStatus _status = new AiLabelingStatus(AiLabelingState.Disabled, 0, 0, "");

        public AiLabelingService(IAppConfiguration appConfiguration, AiModelManager modelManager,
            ILocalVideosService localVideosService, IMediaFrameExtractor frameExtractor)
        {
            _appConfiguration = appConfiguration;
            _modelManager = modelManager;
            _localVideosService = localVideosService;
            _frameExtractor = frameExtractor;
        }

        public AiLabelingStatus Status => _status;

        public void Start()
        {
            if (_worker != null) return;

            StrongReferenceMessenger.Default.Register<AiLabelingService, DownloadStatusChanged>(this, (r, m) =>
            {
                if (m.DownloadStatus == Models.DownloadStatus.Downloaded)
                {
                    r.RequestRun(AiRunKind.Incremental, DownloadDelay);
                }
            });
            StrongReferenceMessenger.Default.Register<AiLabelingService, CategoriesChanged>(this, (r, m) =>
            {
                r.RequestRun(AiRunKind.RescoreOnly, CategoriesDelay);
            });

            _worker = Task.Run(WorkerLoop);

            if (_appConfiguration.AiLabelsEnabled)
            {
                SetStatus(AiLabelingState.Idle, 0, 0, "Waiting");
                RequestRun(AiRunKind.Incremental, StartupDelay);
            }
        }

        public void Enable()
        {
            _appConfiguration.AiLabelsEnabled = true;
            SetStatus(AiLabelingState.Idle, 0, 0, "Waiting");
            RequestRun(AiRunKind.Incremental, TimeSpan.Zero);
        }

        public void Disable()
        {
            _appConfiguration.AiLabelsEnabled = false;
            Cancel();
            SetStatus(AiLabelingState.Disabled, 0, 0, "");
        }

        public void SetAnalyzeVideoFrames(bool value)
        {
            if (_appConfiguration.AiAnalyzeVideoFrames == value) return;
            _appConfiguration.AiAnalyzeVideoFrames = value;
            if (!value && _status.State is AiLabelingState.EmbeddingImages or AiLabelingState.Downloading)
            {
                // Stop analyzing frames (or downloading the vision model), the new run uses only text.
                Cancel();
            }
            RequestRun(AiRunKind.Incremental, TimeSpan.Zero);
        }

        public void RequestRun(AiRunKind kind, TimeSpan delay)
        {
            if (!_appConfiguration.AiLabelsEnabled) return;

            lock (_lock)
            {
                var dueAt = DateTime.Now + delay;
                if (_pending == null || delay == TimeSpan.Zero || dueAt > _dueAt)
                {
                    _dueAt = dueAt;
                }
                _pending = _pending == null || kind > _pending ? kind : _pending;
            }
            _signal.Release();
        }

        public async Task RecomputeAllAsync()
        {
            Cancel();
            List<int> videoIds;
            using (var db = new TikTokDbContext())
            {
                videoIds = await db.VideoAiLabels.Where(x => x.Status == AiLabelStatus.Suggested).Select(x => x.VideoId).Distinct().ToListAsync();
                await db.VideoAiLabels.Where(x => x.Status == AiLabelStatus.Suggested).ExecuteDeleteAsync();
            }
            if (videoIds.Count > 0)
            {
                StrongReferenceMessenger.Default.Send(new AiLabelsChanged(videoIds));
            }
            RequestRun(AiRunKind.Incremental, TimeSpan.Zero);
        }

        public void Cancel()
        {
            lock (_lock)
            {
                _pending = null;
                _runCts?.Cancel();
            }
        }

        private async Task WorkerLoop()
        {
            while (true)
            {
                await _signal.WaitAsync().ConfigureAwait(false);

                // Debounce: wait until the request is due, new requests can move the due time.
                bool hasPending;
                while (true)
                {
                    TimeSpan wait;
                    lock (_lock)
                    {
                        hasPending = _pending != null;
                        wait = _dueAt - DateTime.Now;
                    }
                    if (!hasPending || wait <= TimeSpan.Zero) break;
                    await _signal.WaitAsync(wait).ConfigureAwait(false);
                }
                if (!hasPending) continue;

                AiRunKind kind;
                CancellationToken cancellationToken;
                lock (_lock)
                {
                    if (_pending == null) continue;
                    kind = _pending.Value;
                    _pending = null;
                    _runCts = new CancellationTokenSource();
                    cancellationToken = _runCts.Token;
                }

                try
                {
                    await RunAsync(kind, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    Log.Information("AI labeling cancelled");
                    if (_appConfiguration.AiLabelsEnabled)
                    {
                        SetStatus(AiLabelingState.Idle, 0, 0, "Cancelled");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "AI labeling failed");
                    SetStatus(AiLabelingState.Error, 0, 0, ex.Message);
                }
                finally
                {
                    lock (_lock)
                    {
                        _runCts.Dispose();
                        _runCts = null;
                    }
                }
            }
        }

        private async Task RunAsync(AiRunKind kind, CancellationToken cancellationToken)
        {
            Log.Information("AI labeling run {Kind}", kind);
            if (!_modelManager.AreModelsPresent())
            {
                SetStatus(AiLabelingState.Downloading, 0, _modelManager.TotalSize, "Downloading AI models");
                await _modelManager.EnsureModelsAsync(
                    new SyncProgress<(long Done, long Total)>(p => SetStatus(AiLabelingState.Downloading, p.Done, p.Total,
                        $"Downloading AI models {p.Done / 1048576} / {p.Total / 1048576} MB")),
                    cancellationToken).ConfigureAwait(false);
            }

            using var context = new RunContext(_modelManager);
            if (kind == AiRunKind.Incremental)
            {
                await EmbedTextAsync(context, cancellationToken).ConfigureAwait(false);
            }

            var labelCount = await ScoreAsync(context, cancellationToken).ConfigureAwait(false);

            // Image vectors computed earlier are still used for scoring when frame analysis is turned off.
            if (kind == AiRunKind.Incremental && _appConfiguration.AiAnalyzeVideoFrames)
            {
                if (await EmbedImagesAsync(context, cancellationToken).ConfigureAwait(false))
                {
                    labelCount = await ScoreAsync(context, cancellationToken).ConfigureAwait(false);
                }
            }

            SetStatus(AiLabelingState.Idle, 0, 0, $"Up to date, {labelCount} AI labels");
        }

        private async Task EmbedTextAsync(RunContext context, CancellationToken cancellationToken)
        {
            List<(int Id, string Text, string Hash)> todo;
            using (var db = new TikTokDbContext())
            {
                var videos = await db.Videos.AsNoTracking()
                    .Select(v => new { v.Id, v.Description, Hashtags = v.Hashtags.Select(h => h.Name).ToList(), MusicTitle = v.Music.Title })
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
                var existing = await db.VideoEmbeddings.AsNoTracking()
                    .Select(e => new { e.VideoId, e.TextHash, e.ModelVersion })
                    .ToDictionaryAsync(e => e.VideoId, cancellationToken).ConfigureAwait(false);

                todo = videos
                    .Select(v =>
                    {
                        var text = VideoTextBuilder.Build(v.Description, v.Hashtags, v.MusicTitle);
                        return (v.Id, Text: text, Hash: VideoTextBuilder.Hash(text));
                    })
                    .Where(x => !existing.TryGetValue(x.Id, out var e) || e.TextHash != x.Hash || e.ModelVersion != AiModelCatalog.ModelVersion)
                    .ToList();
            }

            for (int start = 0; start < todo.Count; start += TextBatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SetStatus(AiLabelingState.EmbeddingText, start, todo.Count, $"Analyzing descriptions {start} / {todo.Count}");

                var batch = todo.Skip(start).Take(TextBatchSize).ToList();
                var withText = batch.Where(x => x.Text.Length > 0).ToList();
                var encoded = context.Text.Encode(withText.Select(x => x.Text).ToList());
                var vectors = withText.Select((x, i) => (x.Id, Vector: encoded[i])).ToDictionary(x => x.Id, x => x.Vector);

                using var db = new TikTokDbContext();
                var ids = batch.Select(x => x.Id).ToList();
                var rows = await db.VideoEmbeddings.Where(e => ids.Contains(e.VideoId))
                    .ToDictionaryAsync(e => e.VideoId, cancellationToken).ConfigureAwait(false);
                foreach (var item in batch)
                {
                    if (!rows.TryGetValue(item.Id, out var row))
                    {
                        row = new VideoEmbedding { VideoId = item.Id };
                        db.VideoEmbeddings.Add(row);
                    }
                    if (row.ModelVersion != AiModelCatalog.ModelVersion)
                    {
                        row.ImageVector = null;
                        row.ImageSource = null;
                    }
                    row.TextVector = VectorMath.ToBlob(vectors.GetValueOrDefault(item.Id));
                    row.TextHash = item.Hash;
                    row.ModelVersion = AiModelCatalog.ModelVersion;
                    row.UpdatedAt = DateTime.Now;
                }
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <returns>True when any image embedding changed.</returns>
        private async Task<bool> EmbedImagesAsync(RunContext context, CancellationToken cancellationToken)
        {
            var todo = new List<(int Id, string VideoPath, List<string> ImagePaths, string Source)>();
            using (var db = new TikTokDbContext())
            {
                var videos = await db.Videos.AsNoTracking().Select(v => new { v.Id, v.VideoId })
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
                var existing = await db.VideoEmbeddings.AsNoTracking()
                    .Select(e => new { e.VideoId, e.ImageSource, e.ModelVersion })
                    .ToDictionaryAsync(e => e.VideoId, cancellationToken).ConfigureAwait(false);

                foreach (var video in videos)
                {
                    var paths = _localVideosService.GetPaths(video.VideoId);
                    var videoPath = paths.FirstOrDefault(x => VideoExtensions.Contains(Path.GetExtension(x)));
                    var imagePaths = videoPath == null ? SelectImages(paths) : new List<string>();
                    if (videoPath == null && imagePaths.Count == 0) continue;

                    var source = GetMediaKey(videoPath != null ? new[] { videoPath } : imagePaths);
                    if (source == null) continue;
                    if (existing.TryGetValue(video.Id, out var e) && e.ModelVersion == AiModelCatalog.ModelVersion
                        && (e.ImageSource == source || e.ImageSource == FailedPrefix + source)) continue;

                    todo.Add((video.Id, videoPath, imagePaths, source));
                }
            }

            var buffer = new List<(int Id, float[] Vector, string Source)>();
            for (int i = 0; i < todo.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                SetStatus(AiLabelingState.EmbeddingImages, i, todo.Count, $"Analyzing video frames {i} / {todo.Count}");

                var item = todo[i];
                float[] vector = null;
                try
                {
                    var frames = new List<RgbImage>();
                    if (item.VideoPath != null)
                    {
                        frames.AddRange(await _frameExtractor.ExtractVideoFramesAsync(item.VideoPath, FramesPerVideo, cancellationToken).ConfigureAwait(false));
                    }
                    foreach (var imagePath in item.ImagePaths)
                    {
                        frames.Add(await _frameExtractor.LoadImageAsync(imagePath, cancellationToken).ConfigureAwait(false));
                    }
                    frames.RemoveAll(x => x == null || x.Width != ClipPreprocessor.Size || x.Height != ClipPreprocessor.Size);
                    if (frames.Count > 0)
                    {
                        vector = VectorMath.Mean(context.Image.Encode(frames));
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "AI labeling: can't extract frames of video {Id} {Path}", item.Id, item.VideoPath ?? item.ImagePaths.FirstOrDefault());
                }

                buffer.Add((item.Id, vector, vector == null ? FailedPrefix + item.Source : item.Source));
                if (buffer.Count >= ImageSaveBatchSize)
                {
                    await SaveImageVectorsAsync(buffer, cancellationToken).ConfigureAwait(false);
                }
                if ((i + 1) % RescoreEveryImages == 0)
                {
                    await SaveImageVectorsAsync(buffer, cancellationToken).ConfigureAwait(false);
                    await ScoreAsync(context, cancellationToken).ConfigureAwait(false);
                }
            }
            await SaveImageVectorsAsync(buffer, cancellationToken).ConfigureAwait(false);
            return todo.Count > 0;
        }

        private static List<string> SelectImages(IReadOnlyList<string> paths)
        {
            var images = paths.Where(x => ImageExtensions.Contains(Path.GetExtension(x))).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
            if (images.Count <= FramesPerVideo) return images;
            return new[] { 0, 1, 2, images.Count - 1 }.Select(i => images[i]).ToList();
        }

        private static string GetMediaKey(IEnumerable<string> paths)
        {
            var parts = new List<string>();
            foreach (var path in paths)
            {
                var info = new FileInfo(path);
                if (!info.Exists) return null;
                parts.Add($"{path}|{info.Length}|{info.LastWriteTimeUtc.Ticks}");
            }
            return VideoTextBuilder.Hash(string.Join(";", parts));
        }

        private static async Task SaveImageVectorsAsync(List<(int Id, float[] Vector, string Source)> buffer, CancellationToken cancellationToken)
        {
            if (buffer.Count == 0) return;

            using var db = new TikTokDbContext();
            var ids = buffer.Select(x => x.Id).ToList();
            var rows = await db.VideoEmbeddings.Where(e => ids.Contains(e.VideoId))
                .ToDictionaryAsync(e => e.VideoId, cancellationToken).ConfigureAwait(false);
            foreach (var item in buffer)
            {
                if (!rows.TryGetValue(item.Id, out var row))
                {
                    row = new VideoEmbedding { VideoId = item.Id };
                    db.VideoEmbeddings.Add(row);
                    rows[item.Id] = row;
                }
                row.ImageVector = VectorMath.ToBlob(item.Vector);
                row.ImageSource = item.Source;
                row.ModelVersion = AiModelCatalog.ModelVersion;
                row.UpdatedAt = DateTime.Now;
            }
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            buffer.Clear();
        }

        /// <returns>Number of suggested labels.</returns>
        private async Task<int> ScoreAsync(RunContext context, CancellationToken cancellationToken)
        {
            SetStatus(AiLabelingState.Scoring, 0, 0, "Matching categories");

            List<AiVideoVectors> videos;
            List<string> categories;
            Dictionary<string, List<int>> positives;
            HashSet<(int, string)> rejected;
            using (var db = new TikTokDbContext())
            {
                var embeddings = await db.VideoEmbeddings.AsNoTracking()
                    .Where(e => e.ModelVersion == AiModelCatalog.ModelVersion)
                    .Select(e => new { e.VideoId, e.TextVector, e.ImageVector })
                    .ToListAsync(cancellationToken).ConfigureAwait(false);
                videos = embeddings
                    .Select(e => new AiVideoVectors { VideoId = e.VideoId, Text = VectorMath.FromBlob(e.TextVector), Image = VectorMath.FromBlob(e.ImageVector) })
                    .Where(x => x.Text != null || x.Image != null)
                    .ToList();

                categories = await db.Categories.AsNoTracking().Select(c => c.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
                positives = (await db.VideoCategories.AsNoTracking().Select(x => new { x.VideoId, x.CategoryName }).ToListAsync(cancellationToken).ConfigureAwait(false))
                    .GroupBy(x => x.CategoryName)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.VideoId).ToList());
                rejected = (await db.VideoAiLabels.AsNoTracking().Where(x => x.Status == AiLabelStatus.Rejected)
                        .Select(x => new { x.VideoId, x.CategoryName }).ToListAsync(cancellationToken).ConfigureAwait(false))
                    .Select(x => (x.VideoId, x.CategoryName))
                    .ToHashSet();
            }

            var missingNames = categories.Where(x => !_nameVectors.ContainsKey(x)).ToList();
            if (missingNames.Count > 0)
            {
                var prompts = missingNames.SelectMany(x => new[] { x, $"a photo of {x}", $"zdjęcie: {x}" }).ToList();
                var encoded = context.Text.Encode(prompts);
                for (int i = 0; i < missingNames.Count; i++)
                {
                    _nameVectors[missingNames[i]] = VectorMath.Mean(encoded.Skip(i * 3).Take(3).ToList());
                }
            }

            var inputs = categories.Select(x => new AiCategoryInput
            {
                Name = x,
                NameVector = _nameVectors[x],
                Positives = positives.GetValueOrDefault(x) ?? new List<int>(),
            }).ToList();

            cancellationToken.ThrowIfCancellationRequested();
            var results = AiLabelScorer.Score(videos, inputs, rejected, _options);
            Log.Information("AI labeling scored {Videos} videos, {Categories} categories, {Labels} labels: {PerCategory}",
                videos.Count, categories.Count, results.Count,
                string.Join(", ", results.GroupBy(x => x.CategoryName).Select(g => $"{g.Key}={g.Count()}")));

            await SaveLabelsAsync(results, cancellationToken).ConfigureAwait(false);
            return results.Count;
        }

        private static async Task SaveLabelsAsync(List<AiLabelResult> results, CancellationToken cancellationToken)
        {
            var changed = new HashSet<int>();
            using (var db = new TikTokDbContext())
            {
                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

                // Re-read inside the transaction, categories may have changed while scoring.
                var categories = (await db.Categories.Select(c => c.Name).ToListAsync(cancellationToken).ConfigureAwait(false)).ToHashSet();
                var assigned = (await db.VideoCategories.Select(x => new { x.VideoId, x.CategoryName }).ToListAsync(cancellationToken).ConfigureAwait(false))
                    .Select(x => (x.VideoId, x.CategoryName))
                    .ToHashSet();
                var existing = await db.VideoAiLabels.ToListAsync(cancellationToken).ConfigureAwait(false);
                var existingMap = existing.ToDictionary(x => (x.VideoId, x.CategoryName));
                var wanted = results
                    .Where(x => categories.Contains(x.CategoryName) && !assigned.Contains((x.VideoId, x.CategoryName)))
                    .ToDictionary(x => (x.VideoId, x.CategoryName));

                foreach (var label in existing.Where(x => x.Status == AiLabelStatus.Suggested && !wanted.ContainsKey((x.VideoId, x.CategoryName))))
                {
                    db.VideoAiLabels.Remove(label);
                    changed.Add(label.VideoId);
                }

                foreach (var (key, result) in wanted)
                {
                    if (existingMap.TryGetValue(key, out var label))
                    {
                        if (label.Status == AiLabelStatus.Rejected) continue;
                        if (Math.Abs(label.Score - result.Confidence) > 0.01f)
                        {
                            label.Score = result.Confidence;
                            changed.Add(label.VideoId);
                        }
                    }
                    else
                    {
                        db.VideoAiLabels.Add(new VideoAiLabel
                        {
                            VideoId = result.VideoId,
                            CategoryName = result.CategoryName,
                            Score = result.Confidence,
                            Status = AiLabelStatus.Suggested,
                            CreatedAt = DateTime.Now,
                        });
                        changed.Add(result.VideoId);
                    }
                }

                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            if (changed.Count > 0)
            {
                StrongReferenceMessenger.Default.Send(new AiLabelsChanged(changed.ToList()));
            }
        }

        private void SetStatus(AiLabelingState state, long done, long total, string message)
        {
            _status = new AiLabelingStatus(state, done, total, message);
            StrongReferenceMessenger.Default.Send(new AiLabelingStatusChanged(_status));
        }

        /// <summary>ONNX sessions of one run, created lazily and released at the end of the run to free memory.</summary>
        private sealed class RunContext : IDisposable
        {
            private static bool _versionLogged;

            private readonly AiModelManager _modelManager;
            private readonly SessionOptions _sessionOptions;
            private TextEncoder _text;
            private ImageEncoder _image;

            public RunContext(AiModelManager modelManager)
            {
                _modelManager = modelManager;
                _sessionOptions = new SessionOptions
                {
                    IntraOpNumThreads = Math.Clamp(Environment.ProcessorCount / 4, 1, 4),
                    InterOpNumThreads = 1,
                    ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                };
                if (!_versionLogged)
                {
                    _versionLogged = true;
                    Log.Information("ONNX Runtime {Version}", OrtEnv.Instance().GetVersionString());
                }
            }

            public TextEncoder Text => _text ??= new TextEncoder(
                _modelManager.GetPath(AiModelCatalog.TextModel),
                _modelManager.GetPath(AiModelCatalog.Vocab),
                _modelManager.GetPath(AiModelCatalog.Dense),
                _sessionOptions);

            public ImageEncoder Image => _image ??= new ImageEncoder(_modelManager.GetPath(AiModelCatalog.VisionModel), _sessionOptions);

            public void Dispose()
            {
                _text?.Dispose();
                _image?.Dispose();
                _sessionOptions.Dispose();
            }
        }

        /// <summary>IProgress that reports synchronously (System.Progress posts to the thread pool and can reorder reports).</summary>
        private sealed class SyncProgress<T> : IProgress<T>
        {
            private readonly Action<T> _action;
            public SyncProgress(Action<T> action) => _action = action;
            public void Report(T value) => _action(value);
        }
    }
}
