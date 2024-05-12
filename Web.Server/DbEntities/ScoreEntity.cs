using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Server.DbEntities
{
    [PrimaryKey(nameof(FixGuid))]
    [Table(name: "score", Schema = "public")]
    public sealed class ScoreEntity
    {
        [Column("fix_guid")]
        public Guid FixGuid { get; set; }

        [Column("value")]
        public int Rating { get; set; }
    }
}
