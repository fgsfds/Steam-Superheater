using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamFDCommon
{
    public class ZipTools
    {
        /// <summary>
        /// Download ZIP
        /// </summary>
        /// <param name="url">ZIP URL</param>
        /// <param name="filePath">Path to ZIP</param>
        public static async Task DownloadFileAsync(Uri url, string filePath)
        {
            var tempFile = filePath + ".temp";

            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    using var stream = await client.GetStreamAsync(url);
                    using var file = new FileStream(tempFile, FileMode.Create);
                    await stream.CopyToAsync(file);
                }
            }
            catch (Exception)
            {
                throw;
            }

            File.Move(tempFile, filePath);
        }


    }
}
