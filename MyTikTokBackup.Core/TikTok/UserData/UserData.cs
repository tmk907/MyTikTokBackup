using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MyTikTokBackup.Core.TikTok.UserData
{
    public class UserData
    {
        [JsonPropertyName("Activity")]
        public Activity Activity { get; set; }

        [JsonPropertyName("Ads and data")]
        public AdsAndData AdsAndData { get; set; }

        [JsonPropertyName("App Settings")]
        public AppSettings AppSettings { get; set; }

        [JsonPropertyName("Comment")]
        public Comment Comment { get; set; }

        [JsonPropertyName("Direct Messages")]
        public DirectMessages DirectMessages { get; set; }

        [JsonPropertyName("Profile")]
        public Profile Profile { get; set; }

        [JsonPropertyName("Video")]
        public Video Video { get; set; }
    }

    public class FavoriteEffectsList
    {
        [JsonPropertyName("Date")]
        public string Date { get; set; }

        [JsonPropertyName("EffectLink")]
        public string EffectLink { get; set; }
    }

    public class FavoriteEffects
    {
        [JsonPropertyName("FavoriteEffectsList")]
        public List<FavoriteEffectsList> FavoriteEffectsList { get; set; }
    }

    public class FavoriteHashtagList
    {
        [JsonPropertyName("Date")]
        public string Date { get; set; }

        [JsonPropertyName("Link")]
        public string Link { get; set; }
    }

    public class FavoriteHashtags
    {
        [JsonPropertyName("FavoriteHashtagList")]
        public List<FavoriteHashtagList> FavoriteHashtagList { get; set; }
    }

    public class FavoriteSoundList
    {
        [JsonPropertyName("Date")]
        public string Date { get; set; }

        [JsonPropertyName("Link")]
        public string Link { get; set; }
    }

    public class FavoriteSounds
    {
        [JsonPropertyName("FavoriteSoundList")]
        public List<FavoriteSoundList> FavoriteSoundList { get; set; }
    }

    public class FavoriteVideoList
    {
        [JsonPropertyName("Date")]
        public string Date { get; set; }

        [JsonPropertyName("Link")]
        public string Link { get; set; }
    }

    public class FavoriteVideos
    {
        [JsonPropertyName("FavoriteVideoList")]
        public List<FavoriteVideoList> FavoriteVideoList { get; set; }
    }

    public class FansList
    {
        [JsonPropertyName("Date")]
        public string Date { get; set; }

        [JsonPropertyName("UserName")]
        public string UserName { get; set; }
    }

    public class FollowerList
    {
        [JsonPropertyName("FansList")]
        public List<FansList> FansList { get; set; }
    }

    public class Following
    {
        [JsonPropertyName("Date")]
        public string Date { get; set; }

        [JsonPropertyName("UserName")]
        public string UserName { get; set; }
    }

    public class FollowingList
    {
        [JsonPropertyName("Following")]
        public List<Following> Following { get; set; }
    }

    public class Hashtag
    {
    }

    public class ItemFavoriteList
    {
        [JsonPropertyName("Date")]
        public string Date { get; set; }

        [JsonPropertyName("VideoLink")]
        public string VideoLink { get; set; }
    }

    public class LikeList
    {
        [JsonPropertyName("ItemFavoriteList")]
        public List<ItemFavoriteList> ItemFavoriteList { get; set; }
    }

    public class LoginHistoryList
    {
        [JsonPropertyName("Date")]
        public string Date { get; set; }

        [JsonPropertyName("IP")]
        public string IP { get; set; }

        [JsonPropertyName("DeviceModel")]
        public string DeviceModel { get; set; }

        [JsonPropertyName("DeviceSystem")]
        public string DeviceSystem { get; set; }

        [JsonPropertyName("NetworkType")]
        public string NetworkType { get; set; }

        [JsonPropertyName("Carrier")]
        public string Carrier { get; set; }
    }

    public class LoginHistory
    {
        [JsonPropertyName("LoginHistoryList")]
        public List<LoginHistoryList> LoginHistoryList { get; set; }
    }

    public class PurchaseHistory
    {
    }

    public class SearchList
    {
        [JsonPropertyName("Date")]
        public string Date { get; set; }

        [JsonPropertyName("SearchTerm")]
        public string SearchTerm { get; set; }
    }

    public class SearchHistory
    {
        [JsonPropertyName("SearchList")]
        public List<SearchList> SearchList { get; set; }
    }

    public class ShareHistoryList
    {
        [JsonPropertyName("Date")]
        public string Date { get; set; }

        [JsonPropertyName("SharedContent")]
        public string SharedContent { get; set; }

        [JsonPropertyName("Link")]
        public string Link { get; set; }

        [JsonPropertyName("Method")]
        public string Method { get; set; }
    }

    public class ShareHistory
    {
        [JsonPropertyName("ShareHistoryList")]
        public List<ShareHistoryList> ShareHistoryList { get; set; }
    }

    public class Status
    {
        [JsonPropertyName("Resolution")]
        public string Resolution { get; set; }

        [JsonPropertyName("AppVersion")]
        public string AppVersion { get; set; }

        [JsonPropertyName("Idfa")]
        public string Idfa { get; set; }

        [JsonPropertyName("GAid")]
        public string GAid { get; set; }

        [JsonPropertyName("OpenUdid")]
        public string OpenUdid { get; set; }

        [JsonPropertyName("Clientudid")]
        public string Clientudid { get; set; }
    }

    public class VideoList
    {
        [JsonPropertyName("Date")]
        public string Date { get; set; }

        [JsonPropertyName("VideoLink")]
        public string VideoLink { get; set; }
    }

    public class VideoBrowsingHistory
    {
        [JsonPropertyName("VideoList")]
        public List<VideoList> VideoList { get; set; }
    }

    public class Activity
    {
        [JsonPropertyName("Favorite Effects")]
        public FavoriteEffects FavoriteEffects { get; set; }

        [JsonPropertyName("Favorite Hashtags")]
        public FavoriteHashtags FavoriteHashtags { get; set; }

        [JsonPropertyName("Favorite Sounds")]
        public FavoriteSounds FavoriteSounds { get; set; }

        [JsonPropertyName("Favorite Videos")]
        public FavoriteVideos FavoriteVideos { get; set; }

        [JsonPropertyName("Follower List")]
        public FollowerList FollowerList { get; set; }

        [JsonPropertyName("Following List")]
        public FollowingList FollowingList { get; set; }

        [JsonPropertyName("Hashtag")]
        public Hashtag Hashtag { get; set; }

        [JsonPropertyName("Like List")]
        public LikeList LikeList { get; set; }

        [JsonPropertyName("Login History")]
        public LoginHistory LoginHistory { get; set; }

        [JsonPropertyName("Purchase History")]
        public PurchaseHistory PurchaseHistory { get; set; }

        [JsonPropertyName("Search History")]
        public SearchHistory SearchHistory { get; set; }

        [JsonPropertyName("Share History")]
        public ShareHistory ShareHistory { get; set; }

        [JsonPropertyName("Status")]
        public Status Status { get; set; }

        [JsonPropertyName("Video Browsing History")]
        public VideoBrowsingHistory VideoBrowsingHistory { get; set; }
    }

    public class AdInterests
    {
        [JsonPropertyName("AdInterestCategories")]
        public string AdInterestCategories { get; set; }
    }

    public class AdsBasedOnDataReceivedFromPartners
    {
        [JsonPropertyName("DataPartnerList")]
        public string DataPartnerList { get; set; }

        [JsonPropertyName("AdvertiserList")]
        public string AdvertiserList { get; set; }
    }

    public class UsageDataFromThirdPartyAppsAndWebsites
    {
    }

    public class AdsAndData
    {
        [JsonPropertyName("Ad Interests")]
        public AdInterests AdInterests { get; set; }

        [JsonPropertyName("Ads Based On Data Received From Partners")]
        public AdsBasedOnDataReceivedFromPartners AdsBasedOnDataReceivedFromPartners { get; set; }

        [JsonPropertyName("Usage Data From Third-Party Apps And Websites")]
        public UsageDataFromThirdPartyAppsAndWebsites UsageDataFromThirdPartyAppsAndWebsites { get; set; }
    }

    public class BlockList
    {
        [JsonPropertyName("Date")]
        public string Date { get; set; }

        [JsonPropertyName("UserName")]
        public string UserName { get; set; }
    }

    public class Block
    {
        [JsonPropertyName("BlockList")]
        public List<BlockList> BlockList { get; set; }
    }

    public class SettingsMap
    {
        [JsonPropertyName("Ads Based on Data Received from Partners")]
        public string AdsBasedOnDataReceivedFromPartners { get; set; }

        [JsonPropertyName("Allow DownLoad")]
        public string AllowDownLoad { get; set; }

        [JsonPropertyName("Allow Others to Find Me")]
        public string AllowOthersToFindMe { get; set; }

        [JsonPropertyName("Filter Comments")]
        public string FilterComments { get; set; }

        [JsonPropertyName("Interests")]
        public string Interests { get; set; }

        [JsonPropertyName("Language")]
        public string Language { get; set; }

        [JsonPropertyName("Personalized Ads")]
        public string PersonalizedAds { get; set; }

        [JsonPropertyName("Private Account")]
        public string PrivateAccount { get; set; }

        [JsonPropertyName("Video Languages Preferences")]
        public string VideoLanguagesPreferences { get; set; }

        [JsonPropertyName("Who Can Duet With Me")]
        public string WhoCanDuetWithMe { get; set; }

        [JsonPropertyName("Who Can Post Comments")]
        public string WhoCanPostComments { get; set; }

        [JsonPropertyName("Who Can React to Me")]
        public string WhoCanReactToMe { get; set; }

        [JsonPropertyName("Who Can Send Me Message")]
        public string WhoCanSendMeMessage { get; set; }

        [JsonPropertyName("Who Can View Videos I Liked")]
        public string WhoCanViewVideosILiked { get; set; }
    }

    public class Settings
    {
        [JsonPropertyName("SettingsMap")]
        public SettingsMap SettingsMap { get; set; }
    }

    public class AppSettings
    {
        [JsonPropertyName("Block")]
        public Block Block { get; set; }

        [JsonPropertyName("Settings")]
        public Settings Settings { get; set; }
    }

    public class CommentsList
    {
        [JsonPropertyName("Date")]
        public string Date { get; set; }

        [JsonPropertyName("Comment")]
        public string Comment { get; set; }
    }

    public class Comments
    {
        [JsonPropertyName("CommentsList")]
        public List<CommentsList> CommentsList { get; set; }
    }

    public class Comment
    {
        [JsonPropertyName("Comments")]
        public Comments Comments { get; set; }
    }

    public class ChatHistoryItem
    {
    }

    public class ChatHistory
    {
        [JsonPropertyName("ChatHistory")]
        public ChatHistoryItem ChatHistoryItem { get; set; }
    }

    public class DirectMessages
    {
        [JsonPropertyName("Chat History")]
        public ChatHistory ChatHistory { get; set; }
    }

    public class ProfileMap
    {
        //[JsonPropertyName("PlatformInfo")]
        //public List<string> PlatformInfo { get; set; }

        [JsonPropertyName("bioDescription")]
        public string BioDescription { get; set; }

        [JsonPropertyName("birthDate")]
        public string BirthDate { get; set; }

        [JsonPropertyName("emailAddress")]
        public string EmailAddress { get; set; }

        [JsonPropertyName("likesReceived")]
        public string LikesReceived { get; set; }

        [JsonPropertyName("profilePhoto")]
        public string ProfilePhoto { get; set; }

        [JsonPropertyName("profileVideo")]
        public string ProfileVideo { get; set; }

        [JsonPropertyName("telephoneNumber")]
        public string TelephoneNumber { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; }
    }

    public class ProfileInformation
    {
        [JsonPropertyName("ProfileMap")]
        public ProfileMap ProfileMap { get; set; }
    }

    public class Profile
    {
        [JsonPropertyName("Profile Information")]
        public ProfileInformation ProfileInformation { get; set; }
    }

    public class Videos
    {
    }

    public class Video
    {
        [JsonPropertyName("Videos")]
        public Videos Videos { get; set; }
    }
}
