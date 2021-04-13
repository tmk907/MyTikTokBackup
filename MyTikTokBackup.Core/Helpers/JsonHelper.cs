using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace MyTikTokBackup.Core.Helpers
{
    public class JsonHelper
    {
        public static async Task<T> DeserializeFile<T>(string path) where T : new()
        {
            try
            {
                using FileStream openStream = File.OpenRead(path);
                return await JsonSerializer.DeserializeAsync<T>(openStream);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return new T();
            }
        }

        public static async Task SerializeFile(string path, object data)
        {
            try
            {
                using FileStream createStream = File.Create(path);
                await JsonSerializer.SerializeAsync(createStream, data);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}
