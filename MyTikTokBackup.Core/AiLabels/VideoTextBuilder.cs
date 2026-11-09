using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MyTikTokBackup.Core.AiLabels
{
    /// <summary>Builds the text that describes a video for the text encoder.</summary>
    public static class VideoTextBuilder
    {
        private static readonly Regex UrlRegex = new Regex(@"https?://\S+", RegexOptions.Compiled);
        private static readonly Regex MentionRegex = new Regex(@"@[\w.]+", RegexOptions.Compiled);
        private static readonly Regex HashtagRegex = new Regex(@"#(\w+)", RegexOptions.Compiled);
        private static readonly Regex WhitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

        /// <summary>Hashtags that say nothing about the content.</summary>
        private static readonly HashSet<string> GenericTags = new(StringComparer.OrdinalIgnoreCase)
        {
            "fyp", "fy", "fypシ", "fypage", "foryou", "foryoupage", "foru", "4u", "4you", "viral", "viralvideo", "trending", "trend",
            "xyzbca", "xyz", "dc", "dlaciebie", "dlaciebiee", "dlaciebieee", "tiktok", "tiktokpolska",
            "capcut", "duet", "stitch", "explore", "explorepage", "parati", "pourtoi", "fürdich", "blowthisup", "goviral",
        };

        private static readonly string[] GenericMusicTitles =
        {
            "original sound", "oryginalny dźwięk", "dźwięk oryginalny", "son original", "sonido original", "originalton", "suono originale",
        };

        public static string Build(string description, IEnumerable<string> hashtags, string musicTitle)
        {
            var text = description ?? "";
            text = UrlRegex.Replace(text, " ");
            text = MentionRegex.Replace(text, " ");

            var usedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            text = HashtagRegex.Replace(text, m =>
            {
                var tag = m.Groups[1].Value;
                if (GenericTags.Contains(tag) || !usedTags.Add(tag)) return " ";
                return tag;
            });

            var parts = new List<string> { text };
            foreach (var tag in hashtags ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(tag) || GenericTags.Contains(tag) || !usedTags.Add(tag)) continue;
                parts.Add(tag);
            }

            if (!string.IsNullOrWhiteSpace(musicTitle)
                && !GenericMusicTitles.Any(x => musicTitle.Contains(x, StringComparison.OrdinalIgnoreCase)))
            {
                parts.Add(musicTitle);
            }

            return WhitespaceRegex.Replace(string.Join(" ", parts), " ").Trim();
        }

        public static string Hash(string text)
        {
            return Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(text ?? "")));
        }
    }
}
