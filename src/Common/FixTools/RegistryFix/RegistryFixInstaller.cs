using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.RegistryFix;
using Common.Helpers;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Common.FixTools.RegistryFix
{
    public class RegistryFixInstaller
    {
        /// <summary>
        /// Install registry fix
        /// </summary>
        /// <param name="game">Game entity</param>
        /// <param name="fix">Fix entity</param>
        /// <returns>Installed fix entity</returns>
        public BaseInstalledFixEntity InstallFix(GameEntity game, RegistryFixEntity fix)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return ThrowHelper.PlatformNotSupportedException<BaseInstalledFixEntity>(string.Empty);
            }

            var valueName = fix.ValueName.Replace("{gamefolder}", game.InstallDir).Replace("\\\\", "\\");

            Logger.Info($"Value name is {valueName}");

            string? oldValueStr = null;

            var oldValue = Registry.GetValue(fix.Key, valueName, null);
            if (oldValue is not null)
            {
                if (fix.ValueType is RegistryValueType.Dword)
                {
                    oldValueStr = ((int)oldValue).ToString();
                }
                else if (fix.ValueType is RegistryValueType.String)
                {
                    oldValueStr = (string)oldValue;
                }
            }

            if (fix.ValueType is RegistryValueType.Dword)
            {
                Registry.SetValue(fix.Key, valueName, int.Parse(fix.NewValueData));
            }
            else if (fix.ValueType is RegistryValueType.String)
            {
                Registry.SetValue(fix.Key, valueName, fix.NewValueData);
            }

            return new RegistryInstalledFixEntity(game.Id, fix.Guid, fix.Version, fix.Key, valueName, oldValueStr);
        }
    }
}
