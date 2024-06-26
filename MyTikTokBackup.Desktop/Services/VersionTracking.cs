﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Windows.ApplicationModel;
using Xamarin.Essentials;

namespace MyTikTokBackup.Desktop.Services
{
    public class VersionTracking
    {
        const string versionTrailKey = "VersionTracking.Trail";
        const string versionsKey = "VersionTracking.Versions";
        const string buildsKey = "VersionTracking.Builds";

        static readonly string sharedName = Preferences.GetPrivatePreferencesSharedName("versiontracking");

        static readonly Dictionary<string, List<string>> versionTrail;

        static VersionTracking()
        {
            IsFirstLaunchEver = !Preferences.ContainsKey(versionsKey, sharedName) || !Preferences.ContainsKey(buildsKey, sharedName);
            if (IsFirstLaunchEver)
            {
                versionTrail = new Dictionary<string, List<string>>
                {
                    { versionsKey, new List<string>() },
                    { buildsKey, new List<string>() }
                };
            }
            else
            {
                versionTrail = new Dictionary<string, List<string>>
                {
                    { versionsKey, ReadHistory(versionsKey).ToList() },
                    { buildsKey, ReadHistory(buildsKey).ToList() }
                };
            }

            IsFirstLaunchForCurrentVersion = !versionTrail[versionsKey].Contains(CurrentVersion);
            if (IsFirstLaunchForCurrentVersion)
            {
                versionTrail[versionsKey].Add(CurrentVersion);
            }

            IsFirstLaunchForCurrentBuild = !versionTrail[buildsKey].Contains(CurrentBuild);
            if (IsFirstLaunchForCurrentBuild)
            {
                versionTrail[buildsKey].Add(CurrentBuild);
            }

            if (IsFirstLaunchForCurrentVersion || IsFirstLaunchForCurrentBuild)
            {
                WriteHistory(versionsKey, versionTrail[versionsKey]);
                WriteHistory(buildsKey, versionTrail[buildsKey]);
            }
        }

        public static bool IsFirstLaunchEver { get; private set; }

        public static bool IsFirstLaunchForCurrentVersion { get; private set; }

        public static bool IsFirstLaunchForCurrentBuild { get; private set; }

        public static string CurrentVersion
        {
            get
            {
                var version = Package.Current.Id.Version;
                return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            }
        }

        public static string CurrentBuild => Package.Current.Id.Version.Build.ToString(CultureInfo.InvariantCulture);

        public static string PreviousVersion => GetPrevious(versionsKey);

        public static string PreviousBuild => GetPrevious(buildsKey);

        public static string FirstInstalledVersion => versionTrail[versionsKey].FirstOrDefault();

        public static string FirstInstalledBuild => versionTrail[buildsKey].FirstOrDefault();

        public static IEnumerable<string> VersionHistory => versionTrail[versionsKey].ToArray();

        public static IEnumerable<string> BuildHistory => versionTrail[buildsKey].ToArray();

        public static bool IsFirstLaunchForVersion(string version)
            => CurrentVersion == version && IsFirstLaunchForCurrentVersion;

        public static bool IsFirstLaunchForBuild(string build)
            => CurrentBuild == build && IsFirstLaunchForCurrentBuild;

        internal static string GetStatus()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("VersionTracking");
            sb.AppendLine($"  IsFirstLaunchEver:              {IsFirstLaunchEver}");
            sb.AppendLine($"  IsFirstLaunchForCurrentVersion: {IsFirstLaunchForCurrentVersion}");
            sb.AppendLine($"  IsFirstLaunchForCurrentBuild:   {IsFirstLaunchForCurrentBuild}");
            sb.AppendLine();
            sb.AppendLine($"  CurrentVersion:                 {CurrentVersion}");
            sb.AppendLine($"  PreviousVersion:                {PreviousVersion}");
            sb.AppendLine($"  FirstInstalledVersion:          {FirstInstalledVersion}");
            sb.AppendLine($"  VersionHistory:                 [{string.Join(", ", VersionHistory)}]");
            sb.AppendLine();
            sb.AppendLine($"  CurrentBuild:                   {CurrentBuild}");
            sb.AppendLine($"  PreviousBuild:                  {PreviousBuild}");
            sb.AppendLine($"  FirstInstalledBuild:            {FirstInstalledBuild}");
            sb.AppendLine($"  BuildHistory:                   [{string.Join(", ", BuildHistory)}]");
            return sb.ToString();
        }

        static string[] ReadHistory(string key)
            => Preferences.Get(key, null, sharedName)?.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];

        static void WriteHistory(string key, IEnumerable<string> history)
            => Preferences.Set(key, string.Join("|", history), sharedName);

        static string GetPrevious(string key)
        {
            var trail = versionTrail[key];
            return trail.Count >= 2 ? trail[trail.Count - 2] : null;
        }
    }
}
