using System;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using MvvmHelpers;
using MyTikTokBackup.Core.Repositories;

namespace MyTikTokBackup.Core.Models
{
    public class TikTokVideo : ObservableObject
    {
        public string Id { get; init; }
        public string Title { get; init; }
        public string FilePath { get; set; }

        public FullMetadata Metadata { get; set; }

        public ObservableRangeCollection<Category> SelectedCategories { get; init; }

        public override bool Equals(object obj)
        {
            return obj is TikTokVideo video &&
                   Id == video.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public string VideoUrl => $@"https://www.tiktok.com/@{Metadata.Author.UniqueId}/video/{Metadata.Id}";
    }
}
