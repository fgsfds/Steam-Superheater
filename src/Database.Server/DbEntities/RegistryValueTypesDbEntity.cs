using Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Server.DbEntities;

[Table(name: "registry_value_types", Schema = "main")]
public sealed class RegistryValueTypesDbEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required RegistryValueTypeEnum Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }
}

