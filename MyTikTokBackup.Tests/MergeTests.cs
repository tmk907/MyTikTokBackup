using System.Collections.Generic;
using MyTikTokBackup.Core.Database;
using NUnit.Framework;

namespace MyTikTokBackup.Tests
{
    public class MergeTests
    {

        private static readonly object[] _sourceLists =
        {
            // new, old, merged
            new object[] {new List<string>() { "2", "1", "3" }, new List<string>() { "2", "1", "3" }, new List<string>() { "2", "1", "3" }},
            new object[] {new List<string>() { "2", "1", "3", "4" }, new List<string>() { "2", "1", "3" }, new List<string>() { "2", "1", "3", "4" }},
            new object[] {new List<string>() { "2", "1", "3" }, new List<string>() { "2", "1", "3", "4" }, new List<string>() { "2", "1", "3", "4" } },
            new object[] {new List<string>() { "2", "1", "3" }, new List<string>() { "3", "4" }, new List<string>() { "2", "1", "3", "4" }},
            new object[] {new List<string>() { "2", "1", "3", "4", "5" }, new List<string>() { "3", "4" }, new List<string>() { "2", "1", "3", "4", "5" }},
            new object[] {new List<string>() { "2", "1" }, new List<string>() { "3", "4" }, new List<string>() { "2", "1", "3", "4" } },
            new object[] {new List<string>() { "2", "1", "3", "5", "6" }, new List<string>() { "3", "4", "5", "7" }, new List<string>() { "2", "1", "3", "5", "6", "4", "7" }},
        };

        [TestCaseSource("_sourceLists")]
        public void Test1(List<string> newVideos, List<string> oldVideos, List<string> expectedResult)
        {
            var helper = new DatabaseHelper();
            var merged = helper.Merge(oldVideos, newVideos);
            CollectionAssert.AreEqual(expectedResult, merged);
        }
    }
}