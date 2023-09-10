using System.Xml.Serialization;
using SteamFDCommon.Helpers;

namespace SteamFDCommon.Config
{
    public class ConfigProvider
    {
        /// <summary>
        /// Current config
        /// </summary>
        public ConfigEntity Config { get; private set; }

        public ConfigProvider()
        {
            Config = ReadConfigFromXml();

            Config.NotifyConfigChanged += SaveConfigXml;
        }

        /// <summary>
        /// Read config from XML or create new XML if it doesn't exist
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        private ConfigEntity ReadConfigFromXml()
        {
            if (!File.Exists(Consts.ConfigFile))
            {
                var newConfig = new ConfigEntity();

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
                throw new NullReferenceException(nameof(config));
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
