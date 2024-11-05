using Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Server.DbEntities;

[Table(name: "event_types", Schema = "main")]
public sealed class EventTypesDbEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required EventTypeEnum Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }
}
