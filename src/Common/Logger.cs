namespace Common
{
    public static class Logger
    {
        private static string LogFile => Path.Combine(Directory.GetCurrentDirectory(), "superheater.log");

        private static readonly object _lock = new();

        static Logger()
        {
            File.WriteAllText(LogFile, string.Empty);
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

                File.AppendAllText(LogFile, message + Environment.NewLine);
            }
        }

        /// <summary>
        /// Upload log file to ftp
        /// </summary>
        public static void UploadLog()
        {
            FilesUploader.UploadFileToFtp("crashlogs", LogFile, DateTime.Now.ToString("dd.MM.yyyy_HH.mm.ss") + ".log");
        }
    }
}
