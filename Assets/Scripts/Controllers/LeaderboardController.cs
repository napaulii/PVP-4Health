using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels; // Make sure this namespace is included

public class LeaderboardController : MonoBehaviour
{
    // A simple container class so your UI gets both the fortress and its group title
    public class LeaderboardEntry
    {
        public Fortress FortressData { get; set; }
        public string GroupTitle { get; set; }
    }

    public async Task<List<LeaderboardEntry>> GetFortressLeaderboardAsync()
    {
        try
        {
            // 1. Fire off both requests simultaneously to save time
            var fortressTask = SupabaseManager.Instance.From<Fortress>()
                .Order(x => x.Level, Postgrest.Constants.Ordering.Descending)
                .Get();

            var groupTask = SupabaseManager.Instance.From<Group>().Get();

            // 2. Wait for both to complete
            await Task.WhenAll(fortressTask, groupTask);

            var fortresses = fortressTask.Result.Models;
            var groups = groupTask.Result.Models;

            // 3. Map groups to a dictionary for O(1) lightning-fast lookups
            var groupDictionary = groups.ToDictionary(g => g.Id, g => g.Title);

            // 4. Combine the data into our leaderboard structural entries
            List<LeaderboardEntry> leaderboard = new List<LeaderboardEntry>();
            foreach (var fortress in fortresses)
            {
                groupDictionary.TryGetValue(fortress.GroupId, out string title);
                
                leaderboard.Add(new LeaderboardEntry
                {
                    FortressData = fortress,
                    GroupTitle = title ?? "Unknown Fortress" // Fallback if no matching group is found
                });
            }

            return leaderboard;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error assembling fortress leaderboard: {e.Message}");
            return new List<LeaderboardEntry>();
        }
    }
}