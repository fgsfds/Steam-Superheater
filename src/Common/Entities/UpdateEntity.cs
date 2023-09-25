using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Entities
{
    public class UpdateEntity
    {
        public Version Version { get; set; }

        public string Description { get; set; }

        public Uri DownloadUrl { get; set; }

        public UpdateEntity(
            Version version,
            string description,
            Uri url)
        {
            Version = version;
            Description = description;
            DownloadUrl = url;
        }
    }
}
