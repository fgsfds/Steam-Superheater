using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Server.Migrations;

/// <inheritdoc />
public sealed partial class AddVersionString : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.AddColumn<int>(
            name: "version_old",
            schema: "main",
            table: "fixes",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        _ = migrationBuilder.Sql("""
            UPDATE main.fixes
            SET version_old = version
            """);

        _ = migrationBuilder.AlterColumn<string>(
            name: "version",
            schema: "main",
            table: "fixes",
            nullable: true);

        _ = migrationBuilder.Sql("""
            UPDATE main.fixes
            SET version = NULL
            """);

        _ = migrationBuilder.AlterColumn<string>(
            name: "version",
            schema: "main",
            table: "fixes",
            type: "text",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "integer");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropColumn(
            name: "version_old",
            schema: "main",
            table: "fixes");

        _ = migrationBuilder.AlterColumn<int>(
            name: "version",
            schema: "main",
            table: "fixes",
            type: "integer",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");
    }
}
