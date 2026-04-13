using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;

public class GroupChallengeController
{
    // 1. CREATE
    public async Task<GroupChallenge> CreateGroupChallengeAsync(long challengeId, long groupId, int timeToComplete)
    {
        try
        {
            var newChallenge = new GroupChallenge
            {
                Status = "Active", // Default status
                Date = DateTime.UtcNow.Date,
                TimeToComplete = timeToComplete,
                ChallengeId = challengeId,
                GroupId = groupId
            };

            var response = await SupabaseManager.Instance.From<GroupChallenge>().Insert(newChallenge);
            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating group challenge: {e.Message}");
            return null;
        }
    }

    // 2. READ ALL (For specific group)
    public async Task<List<GroupChallenge>> GetChallengesForGroupAsync(long groupId)
    {
        try
        {
            var response = await SupabaseManager.Instance.From<GroupChallenge>()
                .Where(x => x.GroupId == groupId)
                .Get();

            return response.Models;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching group challenges: {e.Message}");
            return new List<GroupChallenge>();
        }
    }

    // 3. READ ONE
    public async Task<GroupChallenge> GetGroupChallengeByIdAsync(long groupChallengeId)
    {
        try
        {
            var response = await SupabaseManager.Instance.From<GroupChallenge>()
                .Where(x => x.Id == groupChallengeId)
                .Get();

            if (response.Models.Count > 0)
            {
                return response.Models[0];
            }
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching group challenge by ID: {e.Message}");
            return null;
        }
    }

    // 4. UPDATE
    public async Task<GroupChallenge> UpdateGroupChallengeStatusAsync(long groupChallengeId, string newStatus)
    {
        try
        {
            var challengeToUpdate = await GetGroupChallengeByIdAsync(groupChallengeId);

            if (challengeToUpdate == null)
            {
                Debug.LogWarning("Cannot update: Group challenge not found.");
                return null;
            }

            challengeToUpdate.Status = newStatus;

            var response = await SupabaseManager.Instance.From<GroupChallenge>().Update(challengeToUpdate);
            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error updating group challenge: {e.Message}");
            return null;
        }
    }

    // 5. DELETE
    public async Task<bool> DeleteGroupChallengeAsync(long groupChallengeId)
    {
        try
        {
            await SupabaseManager.Instance.From<GroupChallenge>()
                .Where(x => x.Id == groupChallengeId)
                .Delete();

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deleting group challenge: {e.Message}");
            return false;
        }
    }
}