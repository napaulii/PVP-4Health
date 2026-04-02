using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SupabaseModels
{
    [Table("personal_item")]
    public class PersonalItem : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("tittle")]
        public string Title { get; set; }

        [Column("description")]
        public string Description { get; set; }
        [Column("price")]
        public int Price { get; set; }

        [Column("fk_userid")]
        public string UserId { get; set; }
    }
}