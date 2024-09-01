using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Blazor.DbEntities;

[PrimaryKey(nameof(Id))]
[Index(nameof(FixGuid))]
[Table(name: "dependencies", Schema = "main")]
public sealed class DependenciesDbEntity
{
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [ForeignKey(nameof(FixesTable))]
    [Column("fix_guid")]
    public required Guid FixGuid { get; set; }

    [ForeignKey(nameof(FixesTable2))]
    [Column("dependency_guid")]
    public required Guid DependencyGuid { get; set; }


    public FixesDbEntity FixesTable { get; set; }
    public FixesDbEntity FixesTable2 { get; set; }
}

