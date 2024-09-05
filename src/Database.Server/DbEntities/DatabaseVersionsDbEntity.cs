using Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Server.DbEntities;

[Table(name: "database_versions", Schema = "main")]
public sealed class DatabaseVersionsDbEntity
{
    [Key]
    [ForeignKey(nameof(DatabaseTablesTable))]
    [Column("id")]
    public required DatabaseTableEnum Id { get; set; }

    [Column("table")]
    public required string Table { get; set; }

    [Column("version")]
    public required int Version { get; set; }


    public DatabaseTablesDbEntity DatabaseTablesTable { get; set; }
}

