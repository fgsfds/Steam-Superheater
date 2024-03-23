using Common.Helpers;

namespace Common
{
    public static class Logger
    {
        private static readonly object _lock = new();
        private static readonly StreamWriter _textWriter;
        private static string LogFile => Path.Combine(Directory.GetCurrentDirectory(), "superheater.log");

        static Logger()
        {
            File.Delete(LogFile);

            _textWriter = new(LogFile, true);
            _textWriter.AutoFlush = true;

            Info(Environment.OSVersion.ToString() + Environment.NewLine + CommonProperties.CurrentVersion.ToString());
        }

        public static void Info(string message) => Log(message, "Info");

        public static void Error(string message) => Log(message, "Error");

        /// <summary>
        /// Add message to the log file
        /// </summary>
        /// <param name="message"></param>
        /// <param name="type">Type of log message</param>
        private static void Log(string message, string type)
        {
            lock (_lock)
            {
                message = $"[{DateTime.Now:dd.MM.yyyy HH.mm.ss}] [{type}] {message}";

                _textWriter.WriteLine(message);
            }
        }

        /// <summary>
        /// Upload log file to ftp
        /// </summary>
        public static void UploadLog()
        {
            FilesUploader.UploadFileToFtp(Consts.CrashlogsFolder, LogFile, DateTime.Now.ToString("dd.MM.yyyy_HH.mm.ss") + ".log");
        }
    }
}
