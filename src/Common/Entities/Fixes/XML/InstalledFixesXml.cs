using Common.Entities.Fixes.FileFix;
using Common.Entities.Fixes.HostsFix;
using Common.Entities.Fixes.RegistryFix;
using System.Xml.Serialization;

namespace Common.Entities.Fixes.XML
{
    [XmlType("InstalledFixes")]
    public sealed class InstalledFixesXml()
    {
        public InstalledFixesXml(List<BaseInstalledFixEntity> fixes) : this()
        {
            InstalledFixes = fixes.ConvertAll(x => (object)x);
        }

        [XmlElement("FileInstalledFix", typeof(FileInstalledFixEntity))]
        [XmlElement("RegistryInstalledFix", typeof(RegistryInstalledFixEntity))]
        [XmlElement("HostsInstalledFix", typeof(HostsInstalledFixEntity))]
        public List<object> InstalledFixes { get; init; }
    }
}
