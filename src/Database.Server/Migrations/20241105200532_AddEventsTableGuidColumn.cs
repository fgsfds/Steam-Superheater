using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Server.Migrations;

/// <inheritdoc />
public sealed partial class AddEventsTableGuidColumn : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.AddColumn<Guid>(
            name: "fix_guid",
            schema: "main",
            table: "events",
            type: "uuid",
            nullable: true);

        _ = migrationBuilder.CreateIndex(
            name: "IX_events_fix_guid",
            schema: "main",
            table: "events",
            column: "fix_guid");

        _ = migrationBuilder.AddForeignKey(
            name: "FK_events_fixes_fix_guid",
            schema: "main",
            table: "events",
            column: "fix_guid",
            principalSchema: "main",
            principalTable: "fixes",
            principalColumn: "guid");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropForeignKey(
            name: "FK_events_fixes_fix_guid",
            schema: "main",
            table: "events");

        _ = migrationBuilder.DropIndex(
            name: "IX_events_fix_guid",
            schema: "main",
            table: "events");

        _ = migrationBuilder.DropColumn(
            name: "fix_guid",
            schema: "main",
            table: "events");
    }
}
