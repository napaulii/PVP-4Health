using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;

public class ChallengeController
{
    // --- READ ---
    public async Task<List<UserChallenge>> GetUserChallengesAsync(string userId)
    {
        try
        {
            // Fetch challenges and JOIN the Challenge table data
            var response = await SupabaseManager.Instance.From<UserChallenge>()
                .Select("*, Challenge(*)")
                .Where(x => x.UserId == userId)
                .Get();
            return response.Models;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching user challenges: {e.Message}");
            return new List<UserChallenge>();
        }
    }

    public async Task<List<GroupChallenge>> GetGroupChallengesAsync(long groupId)
    {
        try
        {
            var response = await SupabaseManager.Instance.From<GroupChallenge>()
                .Select("*, Challenge(*)")
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

    // --- UPDATE (Complete) ---
    public async Task<bool> CompleteUserChallengeAsync(UserChallenge uc)
    {
        try
        {
            // Change the status to completed
            uc.Status = "completed";

            // Send the updated model back to Supabase
            await SupabaseManager.Instance.From<UserChallenge>().Update(uc);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error completing user challenge: {e.Message}");
            return false;
        }
    }

    public async Task<bool> CompleteGroupChallengeAsync(GroupChallenge gc)
    {
        try
        {
            gc.Status = "completed";
            await SupabaseManager.Instance.From<GroupChallenge>().Update(gc);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error completing group challenge: {e.Message}");
            return false;
        }
    }
}