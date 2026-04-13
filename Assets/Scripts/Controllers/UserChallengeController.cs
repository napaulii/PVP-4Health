using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;

public class UserChallengeController
{
    // 1. CREATE
    public async Task<UserChallenge> CreateUserChallengeAsync(long challengeId, int timeToComplete)
    {
        try
        {
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;

            var newChallenge = new UserChallenge
            {
                Status = "Active", // Default status
                Date = DateTime.UtcNow.Date,
                TimeToComplete = timeToComplete,
                UserId = currentUserId,
                ChallengeId = challengeId
            };

            var response = await SupabaseManager.Instance.From<UserChallenge>().Insert(newChallenge);
            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating user challenge: {e.Message}");
            return null;
        }
    }

    // 2. READ ALL (For Current User)
    public async Task<List<UserChallenge>> GetAllUserChallengesAsync()
    {
        try
        {
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;

            var response = await SupabaseManager.Instance.From<UserChallenge>()
                .Where(x => x.UserId == currentUserId)
                .Get();

            return response.Models;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching user challenges: {e.Message}");
            return new List<UserChallenge>();
        }
    }

    // 3. READ ONE
    public async Task<UserChallenge> GetUserChallengeByIdAsync(long userChallengeId)
    {
        try
        {
            var response = await SupabaseManager.Instance.From<UserChallenge>()
                .Where(x => x.Id == userChallengeId)
                .Get();

            if (response.Models.Count > 0)
            {
                return response.Models[0];
            }
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching user challenge by ID: {e.Message}");
            return null;
        }
    }

    // 4. UPDATE
    public async Task<UserChallenge> UpdateUserChallengeStatusAsync(long userChallengeId, string newStatus)
    {
        try
        {
            var challengeToUpdate = await GetUserChallengeByIdAsync(userChallengeId);

            if (challengeToUpdate == null)
            {
                Debug.LogWarning("Cannot update: User challenge not found.");
                return null;
            }

            challengeToUpdate.Status = newStatus;

            var response = await SupabaseManager.Instance.From<UserChallenge>().Update(challengeToUpdate);
            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error updating user challenge: {e.Message}");
            return null;
        }
    }

    // 5. DELETE
    public async Task<bool> DeleteUserChallengeAsync(long userChallengeId)
    {
        try
        {
            await SupabaseManager.Instance.From<UserChallenge>()
                .Where(x => x.Id == userChallengeId)
                .Delete();

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deleting user challenge: {e.Message}");
            return false;
        }
    }
}