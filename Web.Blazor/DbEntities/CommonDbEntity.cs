using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Blazor.DbEntities;

[PrimaryKey(nameof(Name))]
[Table(name: "common", Schema = "main")]
public sealed class CommonDbEntity
{
    [Column("name")]
    public required string Name { get; set; }

    [Column("value")]
    public required string Value { get; set; }
}

