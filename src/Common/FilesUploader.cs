using FluentFTP;

namespace Common
{
    internal static class FilesUploader
    {
        private const string FtpAddress = "31.31.198.106";
        private const string FtpUser = "u2220544_Upload";
        private const string FtpPassword = "YdBunW64d447Pby";

        /// <summary>
        /// Upload single file to ftp
        /// </summary>
        /// <param name="folder">Destination folder on ftp server</param>
        /// <param name="filePath">Path to file to upload</param>
        /// <param name="remoteFileName">File name on the ftp server</param>
        /// <returns>True if successfully uploaded</returns>
        public static bool UploadFileToFtp(string folder, string filePath, string remoteFileName)
        {
            return UploadFilesToFtp(folder, new List<string>() { filePath }, remoteFileName);
        }

        /// <summary>
        /// Upload multiple files to ftp
        /// </summary>
        /// <param name="folder">Destination folder on ftp server</param>
        /// <param name="files">List of paths to files</param>
        /// <returns>True if successfully uploaded</returns>
        public static bool UploadFilesToFtp(string folder, List<string> files, string? remoteFileName = null)
        {
            var client = new FtpClient(FtpAddress, FtpUser, FtpPassword);

            client.CreateDirectory(folder);

            foreach (var file in files)
            {
                var fileName = remoteFileName is null ? Path.GetFileName(file) : remoteFileName;

                var status = client.UploadFile(file, $"{folder}/{fileName}");

                if (status is FtpStatus.Failed)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
