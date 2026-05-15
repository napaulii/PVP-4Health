using Postgrest.Attributes;
using Postgrest.Models;

namespace SupabaseModels
{
    [Table("achievement_definition")]
    public class AchievementDefinition : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("text")]
        public string Text { get; set; }

        [Column("achievement_type")]
        public string AchievementType { get; set; }

        [Column("target_value")]
        public int TargetValue { get; set; }

        [Column("xp_reward")]
        public int XpReward { get; set; }

        [Column("balance_reward")]
        public int BalanceReward { get; set; }
    }
}