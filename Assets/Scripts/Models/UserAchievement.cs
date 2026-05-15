using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SupabaseModels
{
    [Table("user_achievement")]
    public class UserAchievement : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("fk_userid")]
        public string UserId { get; set; }

        [Column("fk_achievementid")]
        public long AchievementId { get; set; }

        [Column("is_unlocked")]
        public bool IsUnlocked { get; set; }

        [Column("is_claimed")]
        public bool IsClaimed { get; set; }
    }
}