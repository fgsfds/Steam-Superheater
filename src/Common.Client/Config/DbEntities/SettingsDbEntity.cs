using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Client.Config.DbEntities;

[Table(name: "settings", Schema = "main")]
public sealed class SettingsDbEntity
{
    [Key]
    [Column("name")]
    public required string Name { get; set; }
    
    [Column("value")]
    public required string Value { get; set; }
}
