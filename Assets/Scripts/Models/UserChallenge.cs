using System;
using Postgrest.Attributes;
using Postgrest.Models;

namespace SupabaseModels
{
    [Table("User_challenge")]
    public class UserChallenge : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("date")]
        public DateTime Date { get; set; }

        [Column("timetocomplete")]
        public int TimeToComplete { get; set; }

        [Column("fk_userid")]
        public string UserId { get; set; }

        [Column("fk_challengeid")]
        public long ChallengeId { get; set; }

        [Reference(typeof(Challenge))]
        public Challenge Challenge { get; set; }
    }
}