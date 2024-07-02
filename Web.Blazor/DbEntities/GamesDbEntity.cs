using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Blazor.DbEntities
{
    [PrimaryKey(nameof(Id))]
    [Index(nameof(Name))]
    [Table(name: "games", Schema = "main")]
    public sealed class GamesDbEntity
    {
        [Column("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public required int Id { get; set; }

        [Column("name")]
        public required string Name { get; set; }
    }
}
