using System.Runtime.CompilerServices;
using Serilog;

namespace MyTikTokBackup.Core.Services
{
    public static class SerilogHelper
    {
        public static void LogInfo(string text, [CallerMemberName] string caller = "")
        {
            Log.Information($"{caller} {text}");
        }
    }
}
