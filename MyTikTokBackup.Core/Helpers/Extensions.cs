using System;
using System.Collections.Generic;

namespace MyTikTokBackup.Core.Helpers
{
    public static class DateTimeExtensions
    {
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    }

    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey,TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            if (dictionary.ContainsKey(key))
            {
                return dictionary[key];
            }
            return defaultValue;
        }
    }
}
