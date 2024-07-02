using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Blazor.DbEntities
{
    [PrimaryKey(nameof(FixGuid))]
    [Table(name: "registry_fixes", Schema = "main")]
    public sealed class RegistryFixesDbEntity
    {
        [ForeignKey(nameof(FixesTable))]
        [Column("fix_guid")]
        public required Guid FixGuid { get; set; }

        [Column("key")]
        public required string Key { get; set; }

        [Column("value_name")]
        public required string ValueName { get; set; }

        [Column("new_value_data")]
        public required string NewValueData { get; set; }

        [Column("value_type_id")]
        public required byte ValueType { get; set; }


        public FixesDbEntity FixesTable { get; set; }
    }
}
