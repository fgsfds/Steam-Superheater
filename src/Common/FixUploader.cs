using FluentFTP;

namespace Common
{
    public static class FixUploader
    {
        private const string FtpAddress = "31.31.198.106";
        private const string FtpUser = "u2220544_Upload";
        private const string FtpPassword = "YdBunW64d447Pby";

        /// <summary>
        /// Upload fix to ftp
        /// </summary>
        /// <param name="folder">Destination folder on ftp server</param>
        /// <param name="files">List of paths to files or tuples with file name and memory stream</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static bool UploadFilesToFtp(string folder, List<object> files)
        {
            List<string> filesList = new();
            List<Tuple<string, MemoryStream>> filesStream = new();

            foreach (var file in files)
            {
                if (file is string fileStr &&
                    File.Exists(fileStr))
                {
                    filesList.Add(fileStr);
                }
                else if (file is Tuple<string, MemoryStream> fileStream)
                {
                    filesStream.Add(fileStream);
                }
                else
                {
                    throw new ArgumentException(nameof(file));
                }
            }

            var client = new FtpClient(FtpAddress, FtpUser, FtpPassword);

            client.CreateDirectory(folder);

            foreach (var file in filesList)
            {
                var status = client.UploadFile(file, $"{folder}/{Path.GetFileName(file)}");

                if (status is FtpStatus.Failed)
                {
                    return false;
                }
            }

            foreach (var stream in filesStream)
            {
                var status = client.UploadStream(stream.Item2, $"{folder}/{stream.Item1}");

                if (status is FtpStatus.Failed)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
