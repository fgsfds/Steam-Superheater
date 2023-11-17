using Common.Entities.Fixes.FileFix;
using System.Xml.Serialization;

namespace Common.Entities.Fixes.XML
{
    [XmlType]
    public sealed class InstalledFixesXml()
    {
        public InstalledFixesXml(List<BaseInstalledFixEntity> fixes) : this()
        {
            InstalledFix = fixes.ConvertAll(x => (object)x);
        }

        [XmlElement("FileInstalledFix", typeof(FileInstalledFixEntity))]
        public List<object> InstalledFix { get; init; }
    }
}
