using Common.Enums;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Database.Server.Migrations
{
    /// <inheritdoc />
    public sealed partial class AddRegFixV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropPrimaryKey(
                name: "PK_registry_fixes",
                schema: "main",
                table: "registry_fixes");

            _ = migrationBuilder.DropIndex(
                name: "IX_games_name",
                schema: "main",
                table: "games");

            _ = migrationBuilder.AddColumn<int>(
                name: "id",
                schema: "main",
                table: "registry_fixes",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            _ = migrationBuilder.AddPrimaryKey(
                name: "PK_registry_fixes",
                schema: "main",
                table: "registry_fixes",
                column: "id");

            _ = migrationBuilder.CreateIndex(
                name: "IX_registry_fixes_fix_guid",
                schema: "main",
                table: "registry_fixes",
                column: "fix_guid");


            _ = migrationBuilder.InsertData(
                table: "fix_types",
                schema: "main",
                columns: ["id", "name"],
                values: [(byte)FixTypeEnum.RegistryFixV2, "Registry fix V2"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropPrimaryKey(
                name: "PK_registry_fixes",
                schema: "main",
                table: "registry_fixes");

            _ = migrationBuilder.DropIndex(
                name: "IX_registry_fixes_fix_guid",
                schema: "main",
                table: "registry_fixes");

            _ = migrationBuilder.DropColumn(
                name: "id",
                schema: "main",
                table: "registry_fixes");

            _ = migrationBuilder.AddPrimaryKey(
                name: "PK_registry_fixes",
                schema: "main",
                table: "registry_fixes",
                column: "fix_guid");

            _ = migrationBuilder.CreateIndex(
                name: "IX_games_name",
                schema: "main",
                table: "games",
                column: "name");


            _ = migrationBuilder.DeleteData(
                table: "fix_types",
                schema: "main",
                keyColumn: "id",
                keyValue: (byte)FixTypeEnum.RegistryFixV2);
        }
    }
}
