using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SupabaseModels
{
    [Table("fortress")]
    public class Fortress : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("state")]
        public string State { get; set; }
        [Column("level")]
        public int Level { get; set; }

        [Column("fk_groupid")]
        public long GroupId { get; set; }
    }
}