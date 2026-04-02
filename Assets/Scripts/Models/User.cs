using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SupabaseModels
{
    [Table("User")]
    public class User : BaseModel
    {
        [PrimaryKey("id", true)]
        public string Id { get; set; }

        [Column("nickname")]
        public string Nickname { get; set; }

        [Column("first_name")]
        public string FirstName { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("balance")]
        public int Balance { get; set; }

        [Column("xp")]
        public int Xp { get; set; }
    }
}