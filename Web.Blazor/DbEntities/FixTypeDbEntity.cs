using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Blazor.DbEntities;

[PrimaryKey(nameof(Id))]
[Table(name: "fix_type", Schema = "main")]
public sealed class FixTypeDbEntity
{
    [Column("id")]
    public byte Id { get; set; }

    [Column("type")]
    public required string Type { get; set; }
}

