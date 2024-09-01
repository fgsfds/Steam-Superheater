using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Blazor.DbEntities;

[PrimaryKey(nameof(FixGuid))]
[Table(name: "installs", Schema = "main")]
public sealed class InstallsDbEntity
{
    [ForeignKey(nameof(FixesTable))]
    [Column("fix_guid")]
    public required Guid FixGuid { get; set; }

    [Column("value")]
    public required int Installs { get; set; }


    public FixesDbEntity FixesTable { get; set; }
}

