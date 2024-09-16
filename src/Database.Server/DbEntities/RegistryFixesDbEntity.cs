using Common.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Server.DbEntities;

[Index(nameof(FixGuid))]
[Table(name: "registry_fixes", Schema = "main")]
public sealed class RegistryFixesDbEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [ForeignKey(nameof(FixesTable))]
    [Column("fix_guid")]
    public required Guid FixGuid { get; set; }

    [Column("key")]
    public required string Key { get; set; }

    [Column("value_name")]
    public required string ValueName { get; set; }

    [Column("new_value_data")]
    public required string NewValueData { get; set; }

    [ForeignKey(nameof(RegistryValueTypesTable))]
    [Column("value_type_id")]
    public required RegistryValueTypeEnum ValueType { get; set; }


    public FixesDbEntity FixesTable { get; set; }
    public RegistryValueTypesDbEntity RegistryValueTypesTable { get; set; }
}

