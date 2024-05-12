using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Server.DbEntities
{
    [PrimaryKey(nameof(Id))]
    [Table(name: "reports", Schema = "public")]
    public sealed class ReportsEntity
    {
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("fix_guid")]
        public required Guid FixGuid { get; set; }

        [Column("text")]
        public required string ReportText { get; set; }
    }
}
