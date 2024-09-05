using Common.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database.Server.DbEntities;

[Index(nameof(GameId))]
[Index(nameof(IsDisabled))]
[Index(nameof(TableVersion))]
[Table(name: "fixes", Schema = "main")]
public sealed class FixesDbEntity
{
    [Key]
    [Column("guid")]
    public required Guid Guid { get; init; }

    [ForeignKey(nameof(GameTable))]
    [Column("game_id")]
    public required int GameId { get; set; }

    [ForeignKey(nameof(FixTypesTable))]
    [Column("fix_type_id")]
    public required FixTypeEnum FixType { get; set; }

    [Column("name")]
    public required string Name { get; set; }

    [Column("version")]
    public required int Version { get; set; }

    [Column("is_windows_supported")]
    public required bool IsWindowsSupported { get; set; }

    [Column("is_linux_supported")]
    public required bool IsLinuxSupported { get; set; }

    [Column("description")]
    public required string? Description { get; set; }

    [Column("changelog")]
    public required string? Changelog { get; set; }

    [Column("notes")]
    public required string? Notes { get; set; }

    [Column("is_disabled")]
    public required bool IsDisabled { get; set; }

    [Column("table_version")]
    public required int TableVersion { get; set; }


    public GamesDbEntity GameTable { get; set; }
    public FixTypesDbEntity FixTypesTable { get; set; }
}

