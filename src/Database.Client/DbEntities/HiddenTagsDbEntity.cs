using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Client.DbEntities;

[Table(name: "hidden_tags")]
public sealed class HiddenTagsDbEntity
{
    [Key]
    [Column("tag")]
    public required string Tag { get; set; }
}