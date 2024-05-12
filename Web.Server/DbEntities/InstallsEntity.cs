using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Server.DbEntities
{
    [PrimaryKey(nameof(FixGuid))]
    [Table(name: "installs", Schema = "public")]
    public sealed class InstallsEntity
    {
        [Column("fix_guid")]
        public required Guid FixGuid { get; set; }

        [Column("value")]
        public required int Installs { get; set; }
    }
}
