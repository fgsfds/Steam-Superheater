using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Server.DbEntities
{
    [PrimaryKey(nameof(Id))]
    [Index(nameof(FixGuid))]
    [Table(name: "reports", Schema = "main")]
    public sealed class ReportsDbEntity
    {
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey(nameof(FixesTable))]
        [Column("fix_guid")]
        public required Guid FixGuid { get; set; }

        [Column("text")]
        public required string ReportText { get; set; }

            
        public FixesDbEntity FixesTable { get; set; }
    }
}
