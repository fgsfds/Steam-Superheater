namespace SteamFDCommon
{
    public class ZipTools
    {
        /// <summary>
        /// Zip downloading progress
        /// </summary>
        public static readonly Progress<float> Progress = new();

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

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);

                using var file = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None);
                using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                using var source = await response.Content.ReadAsStreamAsync();
                var contentLength = response.Content.Headers.ContentLength;

                if (Progress is null || !contentLength.HasValue)
                {
                    await source.CopyToAsync(file);

                    return;
                }

                var progressWrapper = new Progress<long>(totalBytes => ((IProgress<float>)Progress).Report(GetProgressPercentage(totalBytes, contentLength.Value)));

                await CopyToWithProgressAsync(source, file, progressWrapper);
            }

            File.Move(tempFile, filePath);
        }

        private static float GetProgressPercentage(float totalBytes, float currentBytes) => (totalBytes / currentBytes) * 100;

        private static async Task CopyToWithProgressAsync(
            Stream source, 
            FileStream destination, 
            IProgress<long> progress
            )
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (!source.CanRead) throw new InvalidOperationException($"'{nameof(source)}' is not readable.");
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (!destination.CanWrite) throw new InvalidOperationException($"'{nameof(destination)}' is not writable.");

            var buffer = new byte[81920];
            long totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = await source.ReadAsync(buffer)) != 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalBytesRead += bytesRead;
                progress.Report(totalBytesRead);
            }
        }
    }
}
