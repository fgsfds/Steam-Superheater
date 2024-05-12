using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Server.DbEntities
{
    [PrimaryKey(nameof(FixGuid))]
    [Table(name: "installs", Schema = "public")]
    public sealed class InstallsEntity
    {
        [Column("fix_guid")]
        public Guid FixGuid { get; set; }

        [Column("value")]
        public int Installs { get; set; }
    }
}
