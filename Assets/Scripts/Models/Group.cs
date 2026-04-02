using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SupabaseModels
{
    [Table("Group")]
    public class Group : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("tittle")]
        public string Title { get; set; }

        [Column("leader")]
        public string Leader { get; set; }

        [Column("fk_userid")]
        public string UserId { get; set; }
    }
}