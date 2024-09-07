using Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Server.DbEntities;

[Table(name: "fix_types", Schema = "main")]
public sealed class FixTypesDbEntity
{
    [Key]
    [Column("id")]
    public required FixTypeEnum FixType { get; set; }

    [Column("type")]
    public required string Type { get; set; }
}

