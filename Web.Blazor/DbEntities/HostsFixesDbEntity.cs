using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Blazor.DbEntities
{
    [PrimaryKey(nameof(FixGuid))]
    [Table(name: "hosts_fixes", Schema = "main")]
    public sealed class HostsFixesDbEntity
    {
        [ForeignKey(nameof(FixesTable))]
        [Column("fix_guid")]
        public required Guid FixGuid { get; set; }

        [Column("entries")]
        public required List<string> Entries { get; set; }


        public FixesDbEntity FixesTable { get; set; }
    }
}
