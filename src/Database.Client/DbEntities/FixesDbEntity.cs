using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Client.DbEntities;

[Table(name: "fixes", Schema = "main")]
public sealed class FixesDbEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Column("last_updated")]
    public required string LastUpdated { get; set; }

    [Column("fixes")]
    public required string Fixes { get; set; }
}