using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Server.DbEntities;

[Index(nameof(TableVersion))]
[Table(name: "news", Schema = "main")]
public sealed class NewsDbEntity
{
    [Key]
    [Column("date")]
    public required DateTime Date { get; set; }

    [Column("content")]
    public required string Content { get; set; }

    [Column("table_version")]
    public int TableVersion { get; set; }
}
