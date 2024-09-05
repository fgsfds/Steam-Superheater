using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Server.DbEntities;

[Table(name: "hosts_fixes", Schema = "main")]
public sealed class HostsFixesDbEntity
{
    [Key]
    [ForeignKey(nameof(FixesTable))]
    [Column("fix_guid")]
    public required Guid FixGuid { get; set; }

    [Column("entries")]
    public required List<string> Entries { get; set; }


    public FixesDbEntity FixesTable { get; set; }
}

