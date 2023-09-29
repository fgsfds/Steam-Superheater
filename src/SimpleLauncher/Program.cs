using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SimpleLauncher
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {
            string pathToExe = ConfigurationManager.AppSettings["exe"];
            string arguments = ConfigurationManager.AppSettings["args"];
            string ignored = ConfigurationManager.AppSettings["ignored"];
            string fileName = Path.GetFileName(pathToExe);

            var workingDirectory = Path.GetDirectoryName(Path.Combine(Directory.GetCurrentDirectory(), pathToExe));
            Directory.SetCurrentDirectory(workingDirectory);

            if (args.Length != 0 && !args.Any(x => x.Contains(ignored)))
            {
                string str = string.Join(" ", args);

                arguments = string.Join(" ", new string[2]
                {
                    arguments,
                    str
                });
            }

            Process.Start(Path.Combine(workingDirectory, fileName), arguments);
        }
    }
}
