using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Server.DbEntities
{
    [PrimaryKey(nameof(Id))]
    [Table(name: "news", Schema = "public")]
    public sealed class NewsDbEntity
    {
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Column("date")]
        public required DateTime Date { get; set; }

        [Column("content")]
        public required string Content { get; set; }
    }
}
