using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SupabaseModels
{
    [Table("group_challenge")]
    public class GroupChallenge : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("date")]
        public DateTime Date { get; set; }
        [Column("timetocomplete")]
        public int TimeToComplete { get; set; }
        [Column("fk_challengeid")]
        public long ChallengeId { get; set; }

        [Column("fk_groupid")]
        public long GroupId { get; set; }

        [Reference(typeof(Challenge))]
        public Challenge Challenge { get; set; }
    }
}