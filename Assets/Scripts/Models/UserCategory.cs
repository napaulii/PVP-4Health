using System;
using Postgrest.Attributes;
using Postgrest.Models;
using System.Collections.Generic;

namespace SupabaseModels
{
        [Table("user_category")]
        public class UserCategory : BaseModel
        {
            [Column("user_id")] public string UserId { get; set; }
            [Column("category_id")] public long CategoryId { get; set; }
        }
}