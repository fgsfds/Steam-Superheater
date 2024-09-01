using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Client.Migrations;

/// <inheritdoc />
public sealed partial class CreateDatabase : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.EnsureSchema(
            name: "main");

        _ = migrationBuilder.CreateTable(
            name: "hidden_tags",
            schema: "main",
            columns: table => new
            {
                tag = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_hidden_tags", x => x.tag);
            });

        _ = migrationBuilder.CreateTable(
            name: "settings",
            schema: "main",
            columns: table => new
            {
                name = table.Column<string>(type: "TEXT", nullable: false),
                value = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_settings", x => x.name);
            });

        _ = migrationBuilder.CreateTable(
            name: "upvotes",
            schema: "main",
            columns: table => new
            {
                fix_guid = table.Column<Guid>(type: "TEXT", nullable: false),
                is_upvoted = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_upvotes", x => x.fix_guid);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropTable(
            name: "hidden_tags",
            schema: "main");

        _ = migrationBuilder.DropTable(
            name: "settings",
            schema: "main");

        _ = migrationBuilder.DropTable(
            name: "upvotes",
            schema: "main");
    }
}

