using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Blazor.DbEntities
{
    [PrimaryKey(nameof(FixGuid))]
    [Table(name: "file_fixes", Schema = "main")]
    public sealed class FileFixesDbEntity
    {
        [ForeignKey(nameof(FixesTable))]
        [Column("fix_guid")]
        public required Guid FixGuid { get; set; }

        [Column("download_url")]
        public required string? Url { get; set; }

        [Column("file_size")]
        public required long? FileSize { get; set; }

        [Column("file_md5")]
        public required string? MD5 { get; set; }

        [Column("install_folder")]
        public required string? InstallFolder { get; set; }

        [Column("config_file")]
        public required string? ConfigFile { get; set; }

        [Column("files_to_delete")]
        public required List<string>? FilesToDelete { get; set; }

        [Column("files_to_backup")]
        public required List<string>? FilesToBackup { get; set; }

        [Column("files_to_patch")]
        public required List<string>? FilesToPatch { get; set; }

        [Column("run_after_install")]
        public required string? RunAfterInstall { get; set; }

        [Column("variants")]
        public required List<string>? Variants { get; set; }

        [Column("shared_fix_guid")]
        [ForeignKey(nameof(FixesTable2))]
        public required Guid? SharedFixGuid { get; set; }

        [Column("shared_fix_install_folder")]
        public required string? SharedFixInstallFolder { get; set; }

        [Column("wine_dll_overrides")]
        public required List<string>? WineDllOverrides { get; set; }


        public FixesDbEntity FixesTable { get; set; }
        public FixesDbEntity FixesTable2 { get; set; }
    }
}
