using Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Server.DbEntities;

[Table(name: "registry_value_types", Schema = "main")]
public sealed class RegistryValueTypesDbEntity
{
    [Key]
    [Column("id")]
    public required RegistryValueTypeEnum Id { get; set; }

    [Column("type")]
    public required string Type { get; set; }
}

