#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Server.DbEntities;

[Index(nameof(FixGuid))]
[Table(name: "tags_lists", Schema = "main")]
public sealed class TagsListsDbEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [ForeignKey(nameof(FixesTable))]
    [Column("fix_guid")]
    public required Guid FixGuid { get; set; }

    [ForeignKey(nameof(TagsTable))]
    [Column("tag_id")]
    public required int TagId { get; set; }


    public FixesDbEntity FixesTable { get; set; }
    public TagsDbEntity TagsTable { get; set; }
}

