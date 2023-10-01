using FluentFTP;

namespace Common
{
    public static class FilesUploader
    {
        private const string FtpAddress = "31.31.198.106";
        private const string FtpUser = "u2220544_Upload";
        private const string FtpPassword = "YdBunW64d447Pby";

        /// <summary>
        /// Upload single file to ftp
        /// </summary>
        /// <param name="folder">Destination folder on ftp server</param>
        /// <param name="filePath">Path to file to upload</param>
        /// <param name="remoteFileName">File name of the ftp server</param>
        /// <returns>True if successfully uploaded</returns>
        public static bool UploadFileToFtp(string folder, string filePath, string remoteFileName)
        {
            return UploadFilesToFtp(folder, new List<object>() { filePath }, remoteFileName);
        }

        /// <summary>
        /// Upload multiple files to ftp
        /// </summary>
        /// <param name="folder">Destination folder on ftp server</param>
        /// <param name="files">List of paths to files or tuples with file name and memory stream</param>
        /// <returns>True if successfully uploaded</returns>
        /// <exception cref="ArgumentException">if object is not a string or tuple</exception>
        public static bool UploadFilesToFtp(string folder, List<object> files, string? remoteFileName = null)
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
                remoteFileName = remoteFileName is not null ? remoteFileName + ".log" : file;

                var status = client.UploadFile(file, $"{folder}/{Path.GetFileName(remoteFileName)}");

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
