#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Common.Axiom.Enums;

namespace Database.Server.DbEntities;

[Table(name: "events", Schema = "main")]
public sealed class EventsDbEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("time")]
    public required DateTime Time { get; set; }

    [ForeignKey(nameof(EventTypesTable))]
    [Column("id_event_type")]
    public required EventTypeEnum EventType { get; set; }

    [Column("version")]
    public required string Version { get; set; }

    [ForeignKey(nameof(FixesTable))]
    [Column("fix_guid")]
    public required Guid? FixGuid { get; set; }


    public EventTypesDbEntity EventTypesTable { get; set; }
    public FixesDbEntity FixesTable { get; set; }
}
