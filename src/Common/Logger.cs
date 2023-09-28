using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class Logger
    {
        private static string _logFile => Path.Combine(Directory.GetCurrentDirectory(), "superheater.log");

        private static object _lock = new();

        public static void Log(string message)
        {
            lock (_lock)
            {
                File.AppendAllText(_logFile, message + Environment.NewLine);
            }
        }
    }
}
