using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.HostsFix;
using Common.Helpers;
using System.Runtime.InteropServices;

namespace Common.FixTools.HostsFix
{
    public class HostsFixInstaller
    {
        /// <summary>
        /// Install Hosts fix
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix entity</param>
        /// <returns>Installed fix entity</returns>
        public BaseInstalledFixEntity InstallFix(GameEntity game, HostsFixEntity fix)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ThrowHelper.PlatformNotSupportedException<BaseInstalledFixEntity>(string.Empty);
            }

            File.AppendAllLinesAsync(Consts.Hosts, fix.Entries);

            return new HostsInstalledFixEntity(game.Id, fix.Guid, fix.Version, fix.Entries);
        }
    }
}
