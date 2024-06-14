using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Client.Config.DbEntities;

[Table(name: "hidden_tags", Schema = "main")]
public sealed class HiddenTagsDbEntity
{
    [Key]
    [Column("tag")]
    public required string Tag { get; set; }
}
