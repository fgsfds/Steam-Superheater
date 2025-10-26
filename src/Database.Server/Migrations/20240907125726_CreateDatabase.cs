#pragma warning disable IDE0058 // Expression value is never used

using Common.Axiom.Enums;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Database.Server.Migrations;

/// <inheritdoc />
public sealed partial class CreateDatabase : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "main");

        migrationBuilder.CreateTable(
            name: "database_versions",
            schema: "main",
            columns: table => new
            {
                id = table.Column<byte>(type: "smallint", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_database_versions", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "fix_types",
            schema: "main",
            columns: table => new
            {
                id = table.Column<byte>(type: "smallint", nullable: false),
                name = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_fix_types", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "games",
            schema: "main",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false),
                name = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_games", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "news",
            schema: "main",
            columns: table => new
            {
                date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                content = table.Column<string>(type: "text", nullable: false),
                table_version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_news", x => x.date);
            });

        migrationBuilder.CreateTable(
            name: "registry_value_types",
            schema: "main",
            columns: table => new
            {
                id = table.Column<byte>(type: "smallint", nullable: false),
                name = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_registry_value_types", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "tags",
            schema: "main",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                tag = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tags", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "fixes",
            schema: "main",
            columns: table => new
            {
                guid = table.Column<Guid>(type: "uuid", nullable: false),
                game_id = table.Column<int>(type: "integer", nullable: false),
                fix_type_id = table.Column<byte>(type: "smallint", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false),
                is_windows_supported = table.Column<bool>(type: "boolean", nullable: false),
                is_linux_supported = table.Column<bool>(type: "boolean", nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                changelog = table.Column<string>(type: "text", nullable: true),
                notes = table.Column<string>(type: "text", nullable: true),
                score = table.Column<int>(type: "integer", nullable: false),
                installs = table.Column<int>(type: "integer", nullable: false),
                is_disabled = table.Column<bool>(type: "boolean", nullable: false),
                table_version = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_fixes", x => x.guid);
                table.ForeignKey(
                    name: "FK_fixes_fix_types_fix_type_id",
                    column: x => x.fix_type_id,
                    principalSchema: "main",
                    principalTable: "fix_types",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_fixes_games_game_id",
                    column: x => x.game_id,
                    principalSchema: "main",
                    principalTable: "games",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "dependencies",
            schema: "main",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                fix_guid = table.Column<Guid>(type: "uuid", nullable: false),
                dependency_guid = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_dependencies", x => x.id);
                table.ForeignKey(
                    name: "FK_dependencies_fixes_dependency_guid",
                    column: x => x.dependency_guid,
                    principalSchema: "main",
                    principalTable: "fixes",
                    principalColumn: "guid",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_dependencies_fixes_fix_guid",
                    column: x => x.fix_guid,
                    principalSchema: "main",
                    principalTable: "fixes",
                    principalColumn: "guid",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "file_fixes",
            schema: "main",
            columns: table => new
            {
                fix_guid = table.Column<Guid>(type: "uuid", nullable: false),
                download_url = table.Column<string>(type: "text", nullable: true),
                file_size = table.Column<long>(type: "bigint", nullable: true),
                file_md5 = table.Column<string>(type: "text", nullable: true),
                install_folder = table.Column<string>(type: "text", nullable: true),
                config_file = table.Column<string>(type: "text", nullable: true),
                files_to_delete = table.Column<List<string>>(type: "text[]", nullable: true),
                files_to_backup = table.Column<List<string>>(type: "text[]", nullable: true),
                files_to_patch = table.Column<List<string>>(type: "text[]", nullable: true),
                run_after_install = table.Column<string>(type: "text", nullable: true),
                variants = table.Column<List<string>>(type: "text[]", nullable: true),
                shared_fix_guid = table.Column<Guid>(type: "uuid", nullable: true),
                shared_fix_install_folder = table.Column<string>(type: "text", nullable: true),
                wine_dll_overrides = table.Column<List<string>>(type: "text[]", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_file_fixes", x => x.fix_guid);
                table.ForeignKey(
                    name: "FK_file_fixes_fixes_fix_guid",
                    column: x => x.fix_guid,
                    principalSchema: "main",
                    principalTable: "fixes",
                    principalColumn: "guid",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_file_fixes_fixes_shared_fix_guid",
                    column: x => x.shared_fix_guid,
                    principalSchema: "main",
                    principalTable: "fixes",
                    principalColumn: "guid");
            });

        migrationBuilder.CreateTable(
            name: "hosts_fixes",
            schema: "main",
            columns: table => new
            {
                fix_guid = table.Column<Guid>(type: "uuid", nullable: false),
                entries = table.Column<List<string>>(type: "text[]", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_hosts_fixes", x => x.fix_guid);
                table.ForeignKey(
                    name: "FK_hosts_fixes_fixes_fix_guid",
                    column: x => x.fix_guid,
                    principalSchema: "main",
                    principalTable: "fixes",
                    principalColumn: "guid",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "registry_fixes",
            schema: "main",
            columns: table => new
            {
                fix_guid = table.Column<Guid>(type: "uuid", nullable: false),
                key = table.Column<string>(type: "text", nullable: false),
                value_name = table.Column<string>(type: "text", nullable: false),
                new_value_data = table.Column<string>(type: "text", nullable: false),
                value_type_id = table.Column<byte>(type: "smallint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_registry_fixes", x => x.fix_guid);
                table.ForeignKey(
                    name: "FK_registry_fixes_fixes_fix_guid",
                    column: x => x.fix_guid,
                    principalSchema: "main",
                    principalTable: "fixes",
                    principalColumn: "guid",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_registry_fixes_registry_value_types_value_type_id",
                    column: x => x.value_type_id,
                    principalSchema: "main",
                    principalTable: "registry_value_types",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "reports",
            schema: "main",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                fix_guid = table.Column<Guid>(type: "uuid", nullable: false),
                text = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_reports", x => x.id);
                table.ForeignKey(
                    name: "FK_reports_fixes_fix_guid",
                    column: x => x.fix_guid,
                    principalSchema: "main",
                    principalTable: "fixes",
                    principalColumn: "guid",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "tags_lists",
            schema: "main",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                fix_guid = table.Column<Guid>(type: "uuid", nullable: false),
                tag_id = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tags_lists", x => x.id);
                table.ForeignKey(
                    name: "FK_tags_lists_fixes_fix_guid",
                    column: x => x.fix_guid,
                    principalSchema: "main",
                    principalTable: "fixes",
                    principalColumn: "guid",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_tags_lists_tags_tag_id",
                    column: x => x.tag_id,
                    principalSchema: "main",
                    principalTable: "tags",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_dependencies_dependency_guid",
            schema: "main",
            table: "dependencies",
            column: "dependency_guid");

        migrationBuilder.CreateIndex(
            name: "IX_dependencies_fix_guid",
            schema: "main",
            table: "dependencies",
            column: "fix_guid");

        migrationBuilder.CreateIndex(
            name: "IX_file_fixes_shared_fix_guid",
            schema: "main",
            table: "file_fixes",
            column: "shared_fix_guid");

        migrationBuilder.CreateIndex(
            name: "IX_fixes_fix_type_id",
            schema: "main",
            table: "fixes",
            column: "fix_type_id");

        migrationBuilder.CreateIndex(
            name: "IX_fixes_game_id",
            schema: "main",
            table: "fixes",
            column: "game_id");

        migrationBuilder.CreateIndex(
            name: "IX_fixes_is_disabled",
            schema: "main",
            table: "fixes",
            column: "is_disabled");

        migrationBuilder.CreateIndex(
            name: "IX_fixes_table_version",
            schema: "main",
            table: "fixes",
            column: "table_version");

        migrationBuilder.CreateIndex(
            name: "IX_games_name",
            schema: "main",
            table: "games",
            column: "name");

        migrationBuilder.CreateIndex(
            name: "IX_news_table_version",
            schema: "main",
            table: "news",
            column: "table_version");

        migrationBuilder.CreateIndex(
            name: "IX_registry_fixes_value_type_id",
            schema: "main",
            table: "registry_fixes",
            column: "value_type_id");

        migrationBuilder.CreateIndex(
            name: "IX_reports_fix_guid",
            schema: "main",
            table: "reports",
            column: "fix_guid");

        migrationBuilder.CreateIndex(
            name: "IX_tags_tag",
            schema: "main",
            table: "tags",
            column: "tag",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_tags_lists_fix_guid",
            schema: "main",
            table: "tags_lists",
            column: "fix_guid");

        migrationBuilder.CreateIndex(
            name: "IX_tags_lists_tag_id",
            schema: "main",
            table: "tags_lists",
            column: "tag_id");


        migrationBuilder.InsertData(
            table: "database_versions",
            schema: "main",
            columns: ["id", "name", "version"],
            values: [(byte)DatabaseTableEnum.Fixes, "Fixes", 0]);

        migrationBuilder.InsertData(
            table: "database_versions",
            schema: "main",
            columns: ["id", "name", "version"],
            values: [(byte)DatabaseTableEnum.News, "News", 0]);


        migrationBuilder.InsertData(
            table: "fix_types",
            schema: "main",
            columns: ["id", "name"],
            values: [(byte)FixTypeEnum.FileFix, "File fix"]);

        migrationBuilder.InsertData(
            table: "fix_types",
            schema: "main",
            columns: ["id", "name"],
            values: [(byte)FixTypeEnum.RegistryFix, "Registry fix"]);

        migrationBuilder.InsertData(
            table: "fix_types",
            schema: "main",
            columns: ["id", "name"],
            values: [(byte)FixTypeEnum.HostsFix, "Hosts fix"]);

        migrationBuilder.InsertData(
            table: "fix_types",
            schema: "main",
            columns: ["id", "name"],
            values: [(byte)FixTypeEnum.TextFix, "Text fix"]);


        migrationBuilder.InsertData(
            table: "registry_value_types",
            schema: "main",
            columns: ["id", "name"],
            values: [(byte)RegistryValueTypeEnum.String, "String"]);

        migrationBuilder.InsertData(
            table: "registry_value_types",
            schema: "main",
            columns: ["id", "name"],
            values: [(byte)RegistryValueTypeEnum.Dword, "Dword"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "database_versions",
            schema: "main");

        migrationBuilder.DropTable(
            name: "dependencies",
            schema: "main");

        migrationBuilder.DropTable(
            name: "file_fixes",
            schema: "main");

        migrationBuilder.DropTable(
            name: "hosts_fixes",
            schema: "main");

        migrationBuilder.DropTable(
            name: "news",
            schema: "main");

        migrationBuilder.DropTable(
            name: "registry_fixes",
            schema: "main");

        migrationBuilder.DropTable(
            name: "reports",
            schema: "main");

        migrationBuilder.DropTable(
            name: "tags_lists",
            schema: "main");

        migrationBuilder.DropTable(
            name: "registry_value_types",
            schema: "main");

        migrationBuilder.DropTable(
            name: "fixes",
            schema: "main");

        migrationBuilder.DropTable(
            name: "tags",
            schema: "main");

        migrationBuilder.DropTable(
            name: "fix_types",
            schema: "main");

        migrationBuilder.DropTable(
            name: "games",
            schema: "main");
    }
}
