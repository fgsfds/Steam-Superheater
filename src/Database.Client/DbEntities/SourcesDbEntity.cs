using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Client.DbEntities;

[Table(name: "sources")]
public sealed class SourcesDbEntity
{
    [Key]
    [Column("url")]
    public required string Url { get; set; }

    [Column("is_enabled")]
    public required bool IsEnabled { get; set; }
}