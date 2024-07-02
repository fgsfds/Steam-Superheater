using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Blazor.DbEntities
{
    [PrimaryKey(nameof(Id))]
    [Index(nameof(FixGuid))]
    [Table(name: "tags_lists", Schema = "main")]
    public sealed class TagsListsDbEntity
    {
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
}
