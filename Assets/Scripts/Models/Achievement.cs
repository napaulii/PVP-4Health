using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SupabaseModels
{
    [Table("achievement")]
    public class Achievement : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("text")]
        public string Text { get; set; }

        [Column("xp_reward")]
        public int XpReward { get; set; }

        [Column("balance_reward")]
        public int BalanceReward { get; set; }

        [Column("fk_userid")]
        public string UserId { get; set; }

        [Column("completed")]
        public bool Completed { get; set; }
    }
}