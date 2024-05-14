using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Server.DbEntities
{
    [PrimaryKey(nameof(Id))]
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
