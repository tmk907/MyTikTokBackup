using System;
using System.Collections.Generic;

namespace MyTikTokBackup.Core.Repositories
{
    public class FullMetadata
    {
        public string Id { get; set; }

        public string Description { get; set; }

        public DateTime CreateTime { get; set; }

        public Video Video { get; set; }

        public Author Author { get; set; }

        public Music Music { get; set; }

        public List<Challenge> Challenges { get; set; }

        public Stats Stats { get; set; }

        public bool OriginalItem { get; set; }

        public bool OfficalItem { get; set; }

        public List<TextExtra> TextExtra { get; set; }

        public bool Secret { get; set; }

        public bool ForFriend { get; set; }

        public bool Digged { get; set; }

        public long ItemCommentStatus { get; set; }

        public bool ShowNotPass { get; set; }

        public bool Vl1 { get; set; }

        public AuthorStats AuthorStats { get; set; }

        public override bool Equals(object obj)
        {
            return obj is FullMetadata metadata &&
                   Id == metadata.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }

    public class Video
    {
        public string Id { get; set; }

        public long Height { get; set; }

        public long Width { get; set; }

        public TimeSpan Duration { get; set; }

        public string Ratio { get; set; }

        public Uri Cover { get; set; }

        public Uri OriginCover { get; set; }

        public Uri DynamicCover { get; set; }

        public Uri PlayAddr { get; set; }

        public Uri DownloadAddr { get; set; }

        public List<string> ShareCover { get; set; }
    }

    public class Author
    {
        public string Id { get; set; }

        public string UniqueId { get; set; }

        public string Nickname { get; set; }

        public Uri AvatarThumb { get; set; }

        public Uri AvatarMedium { get; set; }

        public Uri AvatarLarger { get; set; }

        public string Signature { get; set; }

        public bool Verified { get; set; }

        public string SecUid { get; set; }

        public long Relation { get; set; }

        public bool OpenFavorite { get; set; }
    }

    public class Music
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public Uri PlayUrl { get; set; }

        public Uri CoverThumb { get; set; }

        public Uri CoverMedium { get; set; }

        public Uri CoverLarge { get; set; }

        public string AuthorName { get; set; }

        public bool Original { get; set; }
    }

    public class Challenge
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string Desc { get; set; }

        public string ProfileThumb { get; set; }

        public string ProfileMedium { get; set; }

        public string ProfileLarger { get; set; }

        public string CoverThumb { get; set; }

        public string CoverMedium { get; set; }

        public string CoverLarger { get; set; }
    }

    public class Stats
    {
        public long DiggCount { get; set; }

        public long ShareCount { get; set; }

        public long CommentCount { get; set; }

        public long PlayCount { get; set; }
    }

    public class TextExtra
    {
        public string AwemeId { get; set; }

        public long Start { get; set; }

        public long End { get; set; }

        public string HashtagName { get; set; }

        public string HashtagId { get; set; }

        public long Type { get; set; }

        public string UserId { get; set; }

        public bool IsCommerce { get; set; }
    }

    public class AuthorStats
    {
        public int FollowingCount { get; set; }

        public int FollowerCount { get; set; }

        public int HeartCount { get; set; }

        public int VideoCount { get; set; }

        public int DiggCount { get; set; }

        public int Heart { get; set; }
    }
}
