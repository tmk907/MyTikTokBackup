using System;

namespace MyTikTokBackup.Core.Database
{
    /// <summary>
    /// Cached embeddings of a video used by AI labeling. Vectors are float32 arrays stored as BLOBs.
    /// </summary>
    public class VideoEmbedding
    {
        /// <summary><see cref="Database.Video.Id"/>.</summary>
        public int VideoId { get; set; }

        public byte[] TextVector { get; set; }
        /// <summary>Hash of the text the vector was computed from.</summary>
        public string TextHash { get; set; }

        public byte[] ImageVector { get; set; }
        /// <summary>Key of the media the vector was computed from ("path|length|ticks"), "failed|..." when extraction failed.</summary>
        public string ImageSource { get; set; }

        public string ModelVersion { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
