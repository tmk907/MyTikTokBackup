using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Serilog;

namespace MyTikTokBackup.Core.TikTok
{
    public class ApiClient
    {
        private const string _sessionIdKey = "sessionid_ss";
        private string _sessionIdValue;

        public void SetSessionIdSs(string sessionid_ss)
        {
            _sessionIdValue = sessionid_ss;
        }

        public async Task<List<Following>> GetMyFollowing(CancellationToken cancellationToken)
        {
            var userList = new List<Following>();
            var url = "https://m.tiktok.com/api/user/list/?aid=1988&count=100&maxCursor=0&minCursor=0";
            try
            {
                var myFollowing = await url.WithCookie(_sessionIdKey, _sessionIdValue).GetJsonAsync<MyFollowing>(cancellationToken);
                userList.AddRange(myFollowing.UserList);
                while (myFollowing?.UserList != null)
                {
                    url = $"https://m.tiktok.com/api/user/list/?aid=1988&count=100&maxCursor={myFollowing.MaxCursor}&minCursor={myFollowing.MinCursor}";
                    myFollowing = await url.WithCookie(_sessionIdKey, _sessionIdValue).GetJsonAsync<MyFollowing>(cancellationToken);
                    userList.AddRange(myFollowing.UserList);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return userList;
        }

        public string GetSessionIdFromCookies(string cookie)
        {
            var sessionId = cookie
                ?.Split(';')?.FirstOrDefault(x => x.Trim().StartsWith("sessionid_ss"))
                ?.Split('=')?.LastOrDefault();
            return sessionId;
        }
    }
}
