using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Common.Axiom.Enums;

namespace Database.Server.DbEntities;

[Table(name: "fix_types", Schema = "main")]
public sealed class FixTypesDbEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required FixTypeEnum Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }
}
