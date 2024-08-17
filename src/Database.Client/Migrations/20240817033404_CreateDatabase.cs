using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Client.Migrations
{
    /// <inheritdoc />
    public partial class CreateDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "main");

            migrationBuilder.CreateTable(
                name: "hidden_tags",
                schema: "main",
                columns: table => new
                {
                    tag = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hidden_tags", x => x.tag);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                schema: "main",
                columns: table => new
                {
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_settings", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "upvotes",
                schema: "main",
                columns: table => new
                {
                    fix_guid = table.Column<Guid>(type: "TEXT", nullable: false),
                    is_upvoted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_upvotes", x => x.fix_guid);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hidden_tags",
                schema: "main");

            migrationBuilder.DropTable(
                name: "settings",
                schema: "main");

            migrationBuilder.DropTable(
                name: "upvotes",
                schema: "main");
        }
    }
}
