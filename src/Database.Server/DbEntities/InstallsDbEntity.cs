using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Server.DbEntities;

[Table(name: "installs", Schema = "main")]
public sealed class InstallsDbEntity
{
    [Key]
    [ForeignKey(nameof(FixesTable))]
    [Column("fix_guid")]
    public required Guid FixGuid { get; set; }

    [Column("value")]
    public required int Installs { get; set; }


    public FixesDbEntity FixesTable { get; set; }
}

