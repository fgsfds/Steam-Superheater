#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Database.Server.DbEntities;

[Index(nameof(FixGuid))]
[Table(name: "reports", Schema = "main")]
public sealed class ReportsDbEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [ForeignKey(nameof(FixesTable))]
    [Column("fix_guid")]
    public required Guid FixGuid { get; set; }

    [Column("text")]
    public required string ReportText { get; set; }


    public FixesDbEntity FixesTable { get; set; }
}
