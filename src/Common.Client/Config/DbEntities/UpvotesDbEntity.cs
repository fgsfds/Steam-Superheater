using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Common.Client.Config.DbEntities;

[Table(name: "upvotes", Schema = "main")]
public sealed class UpvotesDbEntity
{
    [Key]
    [Column("fix_guid")]
    public required Guid FixGuid { get; set; }
    
    [Column("is_upvoted")]
    public required bool IsUpvoted { get; set; }
}
