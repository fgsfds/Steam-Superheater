using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.RegistryFix;
using Common.Helpers;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Common.FixTools.RegistryFix
{
    public sealed class RegistryFixInstaller()
    {
        /// <summary>
        /// Install fix: download ZIP, backup and delete files if needed, run post install events
        /// </summary>
        public BaseInstalledFixEntity InstallFix(GameEntity game, RegistryFixEntity fix)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return ThrowHelper.PlatformNotSupportedException<BaseInstalledFixEntity>(string.Empty);

            var valueName = fix.ValueName.Replace("{installfolder}", game.InstallDir).Replace("\\\\", "\\");

            var oldValue = (string?)Registry.GetValue(fix.Key, valueName, null);

            Registry.SetValue(fix.Key, valueName, fix.NewValueData);

            return new RegistryInstalledFixEntity(game.Id, fix.Guid, fix.Version, fix.Key, valueName, oldValue);
        }
    }
}
