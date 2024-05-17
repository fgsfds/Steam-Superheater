using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Server.DbEntities
{
    [PrimaryKey(nameof(Guid))]
    [Index(nameof(GameId))]
    [Index(nameof(IsDisabled))]
    [Table(name: "fixes", Schema = "main")]
    public sealed class FixesDbEntity
    {
        [Column("guid")]
        public required Guid Guid { get; init; }

        [ForeignKey(nameof(GameTable))]
        [Column("game_id")]
        public int GameId { get; set; }

        [ForeignKey(nameof(FixTypesTable))]
        [Column("fix_type_id")]
        public byte FixType { get; set; }

        [Column("name")]
        public required string Name { get; set; }

        [Column("version")]
        public required int Version { get; set; }

        [Column("is_windows_supported")]
        public required bool IsWindowsSupported { get; set; }

        [Column("is_linux_supported")]
        public required bool IsLinuxSupported { get; set; }

        [Column("description")]
        public string? Description { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("is_disabled")]
        public bool IsDisabled { get; set; }


        public GamesDbEntity GameTable { get; set; }
        public FixTypeDbEntity FixTypesTable { get; set; }
    }
}
