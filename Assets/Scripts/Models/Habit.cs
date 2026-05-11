using System;
using Postgrest.Attributes;
using Postgrest.Models;
using System.Collections.Generic;

namespace SupabaseModels
{
    [Table("habit")]
    public class Habit : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("description")]
        public string Description { get; set; }
        private DateTime _dateOfCreation;
        [Column("dateofcreation")]
        public DateTime DateOfCreation
        {
            get => _dateOfCreation;
            set => _dateOfCreation = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        [Column("longeststreak")]
        public int LongestStreak { get; set; }

        [Column("currentstreak")]
        public int CurrentStreak { get; set; }

        [Column("iscompletedtoday")]
        public bool IsCompletedToday { get; set; }

        private DateTime _lastUpdate;
        [Column("lasttimeupdatedcompletionlist")]
        public DateTime LastTimeUpdatedCompletionList
        {
            get => _lastUpdate;
            set => _lastUpdate = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }


        [Column("completiondatalist")]
        public List<bool> CompletionDataList { get; set; }

        [Column("fk_userid")]
        public string UserId { get; set; }
    }
}