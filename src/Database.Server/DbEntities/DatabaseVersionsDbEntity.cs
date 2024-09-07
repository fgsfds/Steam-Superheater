using Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Server.DbEntities;

[Table(name: "database_versions", Schema = "main")]
public sealed class DatabaseVersionsDbEntity
{
    [Key]
    [Column("id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public required DatabaseTableEnum Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("version")]
    public required int Version { get; set; }
}

