using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;

namespace MyTikTokBackup.Core.Helpers
{
    public class JsonHelper2
    {
        public static async Task<T> DeserializeFile<T>(string path) where T : new()
        {
            try
            {
                using (StreamReader file = File.OpenText(path))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    var data = (T)serializer.Deserialize(file, typeof(T));
                    return await Task.FromResult(data);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return new T();
            }
        }

        //public static async Task SerializeFile(string path, object data)
        //{
        //    try
        //    {
        //        using FileStream createStream = File.Create(path);
        //        await JsonSerializer.SerializeAsync(createStream, data);
        //    }
        //    catch (Exception ex)
        //    {
        //    }
        //}
    }
}
