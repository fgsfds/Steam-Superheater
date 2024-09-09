using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Client.DbEntities;

[Table(name: "upvotes")]
public sealed class UpvotesDbEntity
{
    [Key]
    [Column("fix_guid")]
    public required Guid FixGuid { get; set; }

    [Column("is_upvoted")]
    public required bool IsUpvoted { get; set; }
}