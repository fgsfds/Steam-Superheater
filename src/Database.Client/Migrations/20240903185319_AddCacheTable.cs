using Common.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Client.Migrations;

/// <inheritdoc />
public sealed partial class AddCacheTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropTable(
            name: "fixes",
            schema: "main");

        _ = migrationBuilder.CreateTable(
            name: "cache",
            schema: "main",
            columns: table => new
            {
                type = table.Column<byte>(type: "INTEGER", nullable: false),
                data = table.Column<string>(type: "TEXT", nullable: false),
                version = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_cache", x => x.type);
            });

        _ = migrationBuilder.InsertData(
            table: "cache", 
            schema: "main",
            columns: ["type", "data", "version"],
            values: [(byte)DatabaseTableEnum.Fixes, "[]", 0]
            );

        _ = migrationBuilder.InsertData(
            table: "cache", 
            schema: "main",
            columns: ["type", "data", "version"],
            values: [(byte)DatabaseTableEnum.News, "[]", 0]
            );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropTable(
            name: "cache",
            schema: "main");

        _ = migrationBuilder.CreateTable(
            name: "fixes",
            schema: "main",
            columns: table => new
            {
                id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                fixes = table.Column<string>(type: "TEXT", nullable: false),
                last_updated = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_fixes", x => x.id);
            });
    }
}
