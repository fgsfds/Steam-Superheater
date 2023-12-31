﻿using Common.Enums;
using System.Diagnostics.CodeAnalysis;

namespace Common.Entities.Fixes.RegistryFix
{
    public sealed class RegistryFixEntity : BaseFixEntity
    {
        [SetsRequiredMembers]
        public RegistryFixEntity()
        {
            Name = string.Empty;
            Version = 1;
            Guid = Guid.NewGuid();
            Description = null;
            Dependencies = null;
            Tags = null;
            SupportedOSes = OSEnum.Windows;

            Key = string.Empty;
            ValueName = string.Empty;
            NewValueData = string.Empty;
            ValueType = RegistryValueTypeEnum.String;
        }

        [SetsRequiredMembers]
        public RegistryFixEntity(BaseFixEntity fix)
        {
            Name = fix.Name;
            Version = fix.Version;
            Guid = fix.Guid;
            Description = fix.Description;
            Dependencies = fix.Dependencies;
            Tags = fix.Tags;
            SupportedOSes = OSEnum.Windows;

            Key = string.Empty;
            ValueName = string.Empty;
            NewValueData = string.Empty;
            ValueType = RegistryValueTypeEnum.String;
        }

        /// <summary>
        /// Registry key
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Registry value name
        /// </summary>
        public string ValueName { get; set; }


        /// <summary>
        /// Registry value
        /// </summary>
        public string NewValueData { get; set; }

        /// <summary>
        /// Value type
        /// </summary>
        public required RegistryValueTypeEnum ValueType { get; set; }
    }
}
