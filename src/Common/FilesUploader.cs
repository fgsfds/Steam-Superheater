#pragma warning disable SYSLIB0014
using Common.Helpers;
using System.Net;

namespace Common
{
    internal static class FilesUploader
    {
        private const string FtpAddress = "ftp://31.31.198.106";
        private const string FtpUser = "u2220544_Upload";
        private const string FtpPassword = "YdBunW64d447Pby";

        /// <summary>
        /// Upload single file to ftp
        /// </summary>
        /// <param name="folder">Destination folder on ftp server</param>
        /// <param name="filePath">Path to file to upload</param>
        /// <param name="remoteFileName">File name on the ftp server</param>
        /// <returns>True if successfully uploaded</returns>
        public static Result UploadFileToFtp(string folder, string filePath, string remoteFileName)
        {
            return UploadFilesToFtp(folder, new List<string>() { filePath }, remoteFileName);
        }

        /// <summary>
        /// Upload multiple files to ftp
        /// </summary>
        /// <param name="folder">Destination folder on ftp server</param>
        /// <param name="files">List of paths to files</param>
        /// <returns>True if successfully uploaded</returns>
        public static Result UploadFilesToFtp(string folder, List<string> files, string? remoteFileName = null)
        {
            try
            {
                if (!folder.Equals(Consts.CrashlogsFolder))
                {
                    var createFolderRequest = WebRequest.Create($"{FtpAddress}/{folder}");
                    createFolderRequest.Method = WebRequestMethods.Ftp.MakeDirectory;
                    createFolderRequest.Credentials = new NetworkCredential(FtpUser, FtpPassword);
                    createFolderRequest.GetResponse();
                }

                foreach (var file in files)
                {
                    var fileName = remoteFileName is null ? Path.GetFileName(file) : remoteFileName;

                    var uploadFileRequest = new WebClient();
                    uploadFileRequest.Credentials = new NetworkCredential(FtpUser, FtpPassword);
                    uploadFileRequest.UploadFile($"{FtpAddress}/{folder}/{fileName}", file);
                }
            }
            catch (Exception ex)
            {
                return new(ResultEnum.Error, ex.Message);
            }

            return new(ResultEnum.Ok, string.Empty);
        }
    }
}
#pragma warning restore SYSLIB0014
