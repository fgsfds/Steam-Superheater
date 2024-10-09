using Common.Entities;
using Common.Entities.Fixes;
using Common.Entities.Fixes.RegistryFix;
using Common.Entities.Fixes.RegistryFixV2;
using Common.Enums;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.Logging;
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
    /// <param name="fix">Fix entity</param>
    /// <returns>Installed fix entity</returns>
    public Result<BaseInstalledFixEntity> InstallFix(
        GameEntity game,
        BaseFixEntity fix
        )
    {
        if (!OperatingSystem.IsWindows())
        {
            return ThrowHelper.ThrowPlatformNotSupportedException<Result<BaseInstalledFixEntity>>(string.Empty);
        }

        if (fix.DoesRequireAdminRights && !ClientProperties.IsAdmin)
        {
            ThrowHelper.ThrowUnauthorizedAccessException("Superheater needs to be run as admin in order to install this fix");
        }

        List<RegistryInstalledEntry> installedEntries = [];

        if (fix is RegistryFixEntity regFix)
        {
            var valueName = regFix.ValueName.Replace("{gamefolder}", game.InstallDir).Replace("\\\\", "\\");

            _logger.LogInformation($"Value name is {valueName}");

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
        else if (fix is RegistryFixV2Entity regFix2)
        {
            foreach (var entry in regFix2.Entries)
            {
                var valueName = entry.ValueName.Replace("{gamefolder}", game.InstallDir).Replace("\\\\", "\\");

                _logger.LogInformation($"Value name is {valueName}");

                string? oldValueStr = null;

                var oldValue = Registry.GetValue(entry.Key, valueName, null);
                if (oldValue is not null)
                {
                    switch (entry.ValueType)
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

                switch (entry.ValueType)
                {
                    case RegistryValueTypeEnum.Dword:
                        Registry.SetValue(entry.Key, valueName, int.Parse(entry.NewValueData));
                        break;
                    case RegistryValueTypeEnum.String:
                        Registry.SetValue(entry.Key, valueName, entry.NewValueData);
                        break;
                    default:
                        ThrowHelper.ThrowArgumentOutOfRangeException($"Unknown type: {entry.ValueType.GetType()}");
                        break;
                }

                installedEntries.Add(new()
                {
                    Key = entry.Key,
                    ValueName = valueName,
                    OriginalValue = oldValueStr,
                    ValueType = entry.ValueType
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
                Guid = fix.Guid,
                Version = fix.Version,
                VersionStr = fix.VersionStr,
                Entries = installedEntries
            },
            Message = "Successfully installed fix"
        };

    }
}

