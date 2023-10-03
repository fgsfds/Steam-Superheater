namespace Common
{
    public static class Logger
    {
        private static string _logFile => Path.Combine(Directory.GetCurrentDirectory(), "superheater.log");

        private static object _lock = new();

        static Logger()
        {
            File.WriteAllText(_logFile, string.Empty);
        }

        /// <summary>
        /// Add message to the log file
        /// </summary>
        /// <param name="message"></param>
        public static void Log(string message)
        {
            lock (_lock)
            {
                message = $"[{DateTime.Now}] {message}";

                File.AppendAllText(_logFile, message + Environment.NewLine);
            }
        }

        /// <summary>
        /// Upload log file to ftp
        /// </summary>
        public static void UploadLog()
        {
            FilesUploader.UploadFileToFtp("crashlogs", _logFile, DateTime.Now.ToString());
        }
    }
}
