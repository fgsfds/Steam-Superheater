using Common.Helpers;
using System.Text.Json;

namespace ClientCommon.Config
{
    public sealed class ConfigProvider
    {
        public ConfigProvider()
        {
            Config = ReadConfigFromXml();

            Config.NotifyConfigChanged += SaveConfigXml;
        }

        /// <summary>
        /// Current config
        /// </summary>
        public ConfigEntity Config { get; private set; }

        /// <summary>
        /// Read config from XML or create new XML if it doesn't exist
        /// </summary>
        private ConfigEntity ReadConfigFromXml()
        {
            if (!File.Exists(Consts.ConfigFile))
            {
                ConfigEntity newConfig = new();

                Config = newConfig;

                SaveConfigXml();

                return newConfig;
            }

            ConfigEntity? config;

            using (FileStream fs = new(Consts.ConfigFile, FileMode.OpenOrCreate))
            {
                config = JsonSerializer.Deserialize(fs, ConfigEntityContext.Default.ConfigEntity);
            }

            config.ThrowIfNull();

            return config;
        }

        /// <summary>
        /// Save current config to XML
        /// </summary>
        private void SaveConfigXml()
        {
            using (FileStream fs = new(Consts.ConfigFile, FileMode.Create))
            {
                JsonSerializer.Serialize(
                   fs,
                   Config,
                   ConfigEntityContext.Default.ConfigEntity
                   );
            }
        }
    }
}
