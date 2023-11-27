using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.RegistryFix;
using Common.Enums;
using Common.Helpers;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Common.FixTools.RegistryFix
{
    public sealed class RegistryFixInstaller
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
                switch (fix.ValueType)
                {
                    case RegistryValueTypeEnum.Dword:
                        oldValueStr = ((int)oldValue).ToString();
                        break;
                    case RegistryValueTypeEnum.String:
                        oldValueStr = (string)oldValue;
                        break;
                    default:
                        ThrowHelper.ArgumentException($"Unknown type: {oldValue.GetType()}");
                        break;
                }
            }

            switch (fix.ValueType)
            {
                case RegistryValueTypeEnum.Dword:
                    Registry.SetValue(fix.Key, valueName, int.Parse(fix.NewValueData));
                    break;
                case RegistryValueTypeEnum.String:
                    Registry.SetValue(fix.Key, valueName, fix.NewValueData);
                    break;
                default:
                    ThrowHelper.ArgumentException($"Unknown type: {fix.ValueType.GetType()}");
                    break;
            }

            return new RegistryInstalledFixEntity()
            {
                GameId = game.Id,
                Guid = fix.Guid,
                Version = fix.Version,
                Key = fix.Key,
                ValueName = valueName,
                OriginalValue = oldValueStr
            };
        }
    }
}
