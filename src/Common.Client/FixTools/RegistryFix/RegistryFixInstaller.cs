using Common.Client.Logger;
using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes.RegistryFixV2;
using Common.Enums;
using CommunityToolkit.Diagnostics;
using Microsoft.Win32;

namespace Common.Client.FixTools.RegistryFix;

public sealed class RegistryFixInstaller
{
    private readonly ILogger _logger;


    public RegistryFixInstaller(ILogger logger)
    {
        _logger = logger;
    }


    /// <summary>
    /// Install registry fix
    /// </summary>
    /// <param name="game">Game entity</param>
    /// <param name="fixes">Fix entity</param>
    /// <returns>Installed fix entity</returns>
    public Result<BaseInstalledFixEntity> InstallFix(
        GameEntity game,
        BaseFixEntity fixes
        )
    {
        if (!OperatingSystem.IsWindows())
        {
            return ThrowHelper.ThrowPlatformNotSupportedException<Result<BaseInstalledFixEntity>>(string.Empty);
        }

        List<RegistryInstalledEntry> installedEntries = [];

        if (fixes is RegistryFixEntity regFix)
        {
            var valueName = regFix.ValueName.Replace("{gamefolder}", game.InstallDir).Replace("\\\\", "\\");

            _logger.Info($"Value name is {valueName}");

            string? oldValueStr = null;

            var oldValue = Registry.GetValue(regFix.Key, valueName, null);
            if (oldValue is not null)
            {
                switch (regFix.ValueType)
                {
                    case RegistryValueTypeEnum.Dword:
                        oldValueStr = ((int)oldValue).ToString();
                        break;
                    case RegistryValueTypeEnum.String:
                        oldValueStr = (string)oldValue;
                        break;
                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException($"Unknown type: {oldValue.GetType()}");
                        break;
                }
            }

            switch (regFix.ValueType)
            {
                case RegistryValueTypeEnum.Dword:
                    Registry.SetValue(regFix.Key, valueName, int.Parse(regFix.NewValueData));
                    break;
                case RegistryValueTypeEnum.String:
                    Registry.SetValue(regFix.Key, valueName, regFix.NewValueData);
                    break;
                default:
                    ThrowHelper.ThrowArgumentOutOfRangeException($"Unknown type: {regFix.ValueType.GetType()}");
                    break;
            }

            installedEntries.Add(new()
            {
                Key = regFix.Key,
                ValueName = valueName,
                OriginalValue = oldValueStr,
                ValueType = regFix.ValueType
            });
        }
        else if (fixes is RegistryFixV2Entity regFix2)
        {
            foreach (var fix in regFix2.Entries)
            {
                var valueName = fix.ValueName.Replace("{gamefolder}", game.InstallDir).Replace("\\\\", "\\");

                _logger.Info($"Value name is {valueName}");

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
                            ThrowHelper.ThrowArgumentOutOfRangeException($"Unknown type: {oldValue.GetType()}");
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
                        ThrowHelper.ThrowArgumentOutOfRangeException($"Unknown type: {fix.ValueType.GetType()}");
                        break;
                }

                installedEntries.Add(new()
                {
                    Key = fix.Key,
                    ValueName = valueName,
                    OriginalValue = oldValueStr,
                    ValueType = fix.ValueType
                });
            }
        }
        else
        {
            ThrowHelper.ThrowNotSupportedException();
        }

        return new()
        {
            ResultEnum = ResultEnum.Success,
            ResultObject = new RegistryInstalledFixV2Entity()
            {
                GameId = game.Id,
                Guid = fixes.Guid,
                Version = fixes.Version,
                VersionStr = fixes.VersionStr,
                Entries = installedEntries
            },
            Message = "Successfully installed fix"
        };

    }
}

