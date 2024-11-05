#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Server.DbEntities;

[Index(nameof(FixGuid))]
[Table(name: "dependencies", Schema = "main")]
public sealed class DependenciesDbEntity
{
    [Key]
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
