using System;
using Postgrest.Models;
using Postgrest.Attributes;

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
        public int TimeToComplete { get; set; } // Represented in days (e.g., 7)

        [Column("fk_groupid")]
        public long GroupId { get; set; }

        [Column("completed_date")]
        public DateTime? CompletedDate { get; set; }

        [Column("target_name")]
        public string TargetName { get; set; }

        [Column("target_latitude")]
        public double? TargetLatitude { get; set; }

        [Column("target_longitude")]
        public double? TargetLongitude { get; set; }

        [Column("step_target")]
        public int? StepTarget { get; set; }

        [Column("step_progress")]
        public int StepProgress { get; set; }

        [Column("travels_completed")]
        public int TravelsCompleted { get; set; }
    }
}