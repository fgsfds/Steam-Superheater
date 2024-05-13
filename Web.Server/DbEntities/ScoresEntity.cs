using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Server.DbEntities
{
    [PrimaryKey(nameof(FixGuid))]
    [Table(name: "scores", Schema = "public")]
    public sealed class ScoresEntity
    {
        [Column("fix_guid")]
        public required Guid FixGuid { get; set; }

        [Column("value")]
        public required int Rating { get; set; }
    }
}
