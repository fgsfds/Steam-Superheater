using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.HostsFix;
using Common.Helpers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Common.FixTools.HostsFix
{
    public sealed class HostsFixInstaller
    {
        /// <summary>
        /// Install Hosts fix
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix entity</param>
        /// <param name="hostsFilePath">Path to hosts file</param>
        /// <returns>Installed fix entity</returns>
        public BaseInstalledFixEntity InstallFix(GameEntity game, HostsFixEntity fix, string hostsFilePath)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ThrowHelper.PlatformNotSupportedException<BaseInstalledFixEntity>(string.Empty);
            }

            try
            {
                if (!CommonProperties.IsAdmin)
                {
                    Process.Start(new ProcessStartInfo { FileName = Environment.ProcessPath, UseShellExecute = true, Verb = "runas" });

                    Environment.Exit(0);
                }
            }
            catch
            {
                ThrowHelper.Exception("Superheater needs to be run as admin in order to install hosts fixes");
            }

            var stringToAdd = string.Empty;

            foreach (var line in fix.Entries)
            {
                stringToAdd += Environment.NewLine + line + $" #{fix.Guid}";
            }

            File.AppendAllText(hostsFilePath, stringToAdd);

            return new HostsInstalledFixEntity()
            {
                GameId = game.Id,
                Guid = fix.Guid,
                Version = fix.Version,
                Entries = [.. fix.Entries]
            };
        }
    }
}
