using System.Diagnostics;

namespace SteamFDUpdater
{
    internal class Program
    {
        static void Main()
        {
            while (Process.GetProcessesByName("SteamFD").Any())
            {
                Console.WriteLine("Waiting for SteamFD to exit...");
                Thread.Sleep(100);
                Console.Clear();
            }

            if (File.Exists(".update"))
            {
                Console.WriteLine("OK");
            }
            else
            {
                Console.WriteLine("Not OK");
            }
        }
    }
}