using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Blazor.DbEntities
{
    [PrimaryKey(nameof(Date))]
    [Table(name: "news", Schema = "main")]
    public sealed class NewsDbEntity
    {
        [Column("date")]
        public required DateTime Date { get; set; }

        [Column("content")]
        public required string Content { get; set; }
    }
}
