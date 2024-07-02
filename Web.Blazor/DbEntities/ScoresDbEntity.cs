using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Blazor.DbEntities
{
    [PrimaryKey(nameof(FixGuid))]
    [Table(name: "scores", Schema = "main")]
    public sealed class ScoresDbEntity
    {
        [ForeignKey(nameof(FixesTable))]
        [Column("fix_guid")]
        public required Guid FixGuid { get; set; }

        [Column("value")]
        public required int Score { get; set; }


        public FixesDbEntity FixesTable { get; set; }
    }
}
