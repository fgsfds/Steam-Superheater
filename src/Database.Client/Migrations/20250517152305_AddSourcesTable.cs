using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Client.Migrations
{
    /// <inheritdoc />
    public partial class AddSourcesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "upvotes",
                schema: "main",
                newName: "upvotes");

            migrationBuilder.RenameTable(
                name: "settings",
                schema: "main",
                newName: "settings");

            migrationBuilder.RenameTable(
                name: "hidden_tags",
                schema: "main",
                newName: "hidden_tags");

            migrationBuilder.RenameTable(
                name: "cache",
                schema: "main",
                newName: "cache");

            migrationBuilder.CreateTable(
                name: "sources",
                columns: table => new
                {
                    url = table.Column<string>(type: "TEXT", nullable: false),
                    is_enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sources", x => x.url);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sources");

            migrationBuilder.EnsureSchema(
                name: "main");

            migrationBuilder.RenameTable(
                name: "upvotes",
                newName: "upvotes",
                newSchema: "main");

            migrationBuilder.RenameTable(
                name: "settings",
                newName: "settings",
                newSchema: "main");

            migrationBuilder.RenameTable(
                name: "hidden_tags",
                newName: "hidden_tags",
                newSchema: "main");

            migrationBuilder.RenameTable(
                name: "cache",
                newName: "cache",
                newSchema: "main");
        }
    }
}
