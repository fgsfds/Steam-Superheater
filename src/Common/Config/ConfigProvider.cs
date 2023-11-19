using Common.Helpers;
using System.Xml.Serialization;

namespace Common.Config
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

            XmlSerializer xmlSerializer = new(typeof(ConfigEntity));

            ConfigEntity? config;

            using (FileStream fs = new(Consts.ConfigFile, FileMode.OpenOrCreate))
            {
                config = xmlSerializer.Deserialize(fs) as ConfigEntity;
            }

            if (config is null)
            {
                ThrowHelper.NullReferenceException(nameof(config));
            }

            return config;
        }

        /// <summary>
        /// Save current config to XML
        /// </summary>
        private void SaveConfigXml()
        {
            XmlSerializer xmlSerializer = new(typeof(ConfigEntity));

            using (FileStream fs = new(Consts.ConfigFile, FileMode.Create))
            {
                xmlSerializer.Serialize(fs, Config);
            }
        }
    }
}
