using FluentFTP;

namespace SteamFDCommon
{
    public static class FixUploader
    {
        public static bool UploadFiles(string folder, List<object> files)
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

            var client = new FtpClient("31.31.198.106", "u2220544_Upload", "YdBunW64d447Pby");

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
