using System;
using Common.Enums;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Database.Server.Migrations;

/// <inheritdoc />
public sealed partial class AddEventsTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.AlterColumn<string>(
            name: "version",
            schema: "main",
            table: "fixes",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text");

        _ = migrationBuilder.CreateTable(
            name: "event_types",
            schema: "main",
            columns: table => new
            {
                id = table.Column<byte>(type: "smallint", nullable: false),
                name = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_event_types", x => x.id);
            });

        _ = migrationBuilder.CreateTable(
            name: "events",
            schema: "main",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                id_event_type = table.Column<byte>(type: "smallint", nullable: false),
                version = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_events", x => x.id);
                _ = table.ForeignKey(
                    name: "FK_events_event_types_id_event_type",
                    column: x => x.id_event_type,
                    principalSchema: "main",
                    principalTable: "event_types",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        _ = migrationBuilder.CreateIndex(
            name: "IX_events_id_event_type",
            schema: "main",
            table: "events",
            column: "id_event_type");


        _ = migrationBuilder.InsertData(
            table: "event_types",
            schema: "main",
            columns: ["id", "name"],
            values: [(byte)EventTypeEnum.GetFixes, "Get Fixes"]);

        _ = migrationBuilder.InsertData(
            table: "event_types",
            schema: "main",
            columns: ["id", "name"],
            values: [(byte)EventTypeEnum.Install, "Install"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropTable(
            name: "events",
            schema: "main");

        _ = migrationBuilder.DropTable(
            name: "event_types",
            schema: "main");

        _ = migrationBuilder.AlterColumn<string>(
            name: "version",
            schema: "main",
            table: "fixes",
            type: "text",
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);
    }
}
