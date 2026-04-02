using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SupabaseModels
{
    [Table("challenge")]
    public class Challenge : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }
        [Column("type")]
        public string Type { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("balance_reward")]
        public int BalanceReward { get; set; }

        [Column("xp_reward")]
        public int XpReward { get; set; }

        [Column("fk_categoryid")]
        public long CategoryId { get; set; }
    }
}