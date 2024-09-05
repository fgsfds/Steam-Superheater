using Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Client.DbEntities;

[Table(name: "cache", Schema = "main")]
public sealed class CacheDbEntity
{
    [Key]
    [Column("type")]
    public DatabaseTableEnum Type { get; set; }

    [Column("data")]
    public required string Data { get; set; }

    [Column("version")]
    public required int Version { get; set; }
}