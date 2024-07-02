using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Web.Blazor.DbEntities
{
    [PrimaryKey(nameof(Id))]
    [Table(name: "registry_value_type", Schema = "main")]
    public sealed class RegistryValueTypeDbEntity
    {
        [Column("id")]
        public byte Id { get; set; }

        [Column("type")]
        public required string Type { get; set; }
    }
}
