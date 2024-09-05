using Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Server.DbEntities;

[Table(name: "database_tables", Schema = "main")]
public sealed class DatabaseTablesDbEntity
{
    [Key]
    [Column("id")]
    public required DatabaseTableEnum Id { get; set; }

    [Column("name")]
    public required string Name { get; set; }
}

