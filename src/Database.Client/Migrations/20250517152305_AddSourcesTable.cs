using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Client.Migrations;

/// <inheritdoc />
public sealed partial class AddSourcesTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.RenameTable(
            name: "upvotes",
            schema: "main",
            newName: "upvotes");

        _ = migrationBuilder.RenameTable(
            name: "settings",
            schema: "main",
            newName: "settings");

        _ = migrationBuilder.RenameTable(
            name: "hidden_tags",
            schema: "main",
            newName: "hidden_tags");

        _ = migrationBuilder.RenameTable(
            name: "cache",
            schema: "main",
            newName: "cache");

        _ = migrationBuilder.CreateTable(
            name: "sources",
            columns: table => new
            {
                url = table.Column<string>(type: "TEXT", nullable: false),
                is_enabled = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_sources", x => x.url);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropTable(
            name: "sources");

        _ = migrationBuilder.EnsureSchema(
            name: "main");

        _ = migrationBuilder.RenameTable(
            name: "upvotes",
            newName: "upvotes",
            newSchema: "main");

        _ = migrationBuilder.RenameTable(
            name: "settings",
            newName: "settings",
            newSchema: "main");

        _ = migrationBuilder.RenameTable(
            name: "hidden_tags",
            newName: "hidden_tags",
            newSchema: "main");

        _ = migrationBuilder.RenameTable(
            name: "cache",
            newName: "cache",
            newSchema: "main");
    }
}
