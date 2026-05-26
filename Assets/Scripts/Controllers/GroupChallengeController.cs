using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;

public class GroupChallengeController
{
    /// <summary>
    /// Retrieves the active weekly challenge, automatically rotating or initializing if necessary.
    /// </summary>
    public async Task<GroupChallenge> GetOrCreateWeeklyGroupChallengeAsync(long groupId)
    {
        try
        {
            DateTime today = DateTime.UtcNow.Date;

            // 1. Fetch the most recent challenge for this group
            var response = await SupabaseManager.Instance.From<GroupChallenge>()
                .Where(x => x.GroupId == groupId)
                .Order("date", Postgrest.Constants.Ordering.Descending)
                .Limit(1)
                .Get();

            GroupChallenge lastChallenge = response.Models.Count > 0 ? response.Models[0] : null;

            // 2. If no challenge exists, generate the initial random challenge
            if (lastChallenge == null)
            {
                Debug.Log($"[GroupChallenge] No challenge history for Group {groupId}. Creating initial challenge...");
                return await GenerateNewGroupChallengeAsync(groupId, today, null); // null = random choice
            }

            // 3. Check if the weekly challenge has expired
            bool isExpired = today >= lastChallenge.Date.AddDays(lastChallenge.TimeToComplete);

            if (isExpired)
            {
                Debug.Log($"[GroupChallenge] Weekly challenge expired. Rotating to next bi-weekly event...");

                if (lastChallenge.Status == "Active")
                {
                    lastChallenge.Status = "Failed";
                    await SupabaseManager.Instance.From<GroupChallenge>().Update(lastChallenge);
                }

                // Alternate the challenge type bi-weekly
                bool lastWasSteps = lastChallenge.StepTarget.HasValue && lastChallenge.StepTarget.Value > 0;
                string nextType = lastWasSteps ? "traveler" : "steps";

                return await GenerateNewGroupChallengeAsync(groupId, today, nextType);
            }

            return lastChallenge;
        }
        catch (Exception e)
        {
            Debug.LogError($"[GroupChallenge] Rotation/Retrieval error: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Contributes a user's newly walked steps to the active group challenge.
    /// </summary>
    public async Task SyncStepsToGroupAsync(long groupId, int newStepsDelta)
    {
        if (newStepsDelta <= 0) return;

        var activeChallenge = await GetOrCreateWeeklyGroupChallengeAsync(groupId);
        if (activeChallenge == null || activeChallenge.Status != "Active") return;

        // Ensure the active weekly challenge is indeed a step challenge
        if (activeChallenge.StepTarget.HasValue && activeChallenge.StepTarget.Value > 0)
        {
            activeChallenge.StepProgress += newStepsDelta;

            if (activeChallenge.StepProgress >= activeChallenge.StepTarget.Value)
            {
                activeChallenge.Status = "completed";
                activeChallenge.CompletedDate = DateTime.UtcNow.Date;
            }

            await SupabaseManager.Instance.From<GroupChallenge>().Update(activeChallenge);
            Debug.Log($"[GroupChallenge] Synced steps to group. Current progress: {activeChallenge.StepProgress}/{activeChallenge.StepTarget.Value}");
        }
    }

    /// <summary>
    /// Increments the collective traveler completion counter by +1.
    /// </summary>
   

    private async Task<GroupChallenge> GenerateNewGroupChallengeAsync(long groupId, DateTime startDate, string forceType)
    {
        string type = forceType;
        if (string.IsNullOrEmpty(type))
        {
            // Randomly choose on first-time generation
            type = UnityEngine.Random.Range(0, 2) == 0 ? "steps" : "traveler";
        }

        var newChallenge = new GroupChallenge
        {
            GroupId = groupId,
            Status = "Active",
            Date = startDate,
            TimeToComplete = 7, // 7 days (1 week)
            CompletedDate = null,
            StepProgress = 0,
            TravelsCompleted = 0
        };

        if (type == "steps")
        {
            newChallenge.TargetName = "Walk 100k steps as a group";
            newChallenge.StepTarget = 100000;
            newChallenge.StepProgress = 0;
            newChallenge.TravelsCompleted = 0;
        }
        else
        {
            // --- THE CHANGE ---
            // Set the group challenge title strictly to "Travel" every time
            newChallenge.TargetName = "Travel";
            newChallenge.StepTarget = 0;
            newChallenge.StepProgress = 0;
            newChallenge.TravelsCompleted = 0;
        }

        var insertResponse = await SupabaseManager.Instance.From<GroupChallenge>().Insert(newChallenge);
        return insertResponse.Models[0];
    }
    public async Task ProgressGroupTravelerAsync(GroupChallenge activeChallenge)
    {
        activeChallenge.TravelsCompleted += 1;

        if (activeChallenge.TravelsCompleted >= 20)
        {
            activeChallenge.Status = "completed";
            activeChallenge.CompletedDate = DateTime.UtcNow.Date;
        }
        else
        {
            // Clear target so the UI automatically generates a new one on refresh
            activeChallenge.TargetName = "";
            activeChallenge.TargetLatitude = null;
            activeChallenge.TargetLongitude = null;
        }

        await SupabaseManager.Instance.From<GroupChallenge>().Update(activeChallenge);
    }
}