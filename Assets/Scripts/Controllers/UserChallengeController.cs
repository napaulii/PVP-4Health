using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;
using Postgrest;
using System.Linq;

public class UserChallengeController
{
    // 1. CREATE
    public async Task<UserChallenge> CreateUserChallengeAsync(long challengeId, int timeToComplete)
    {
        Debug.Log("CreateUserChallengeAsync() invoked");
        try
        {
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;
            Debug.Log("Current user id: " + currentUserId);
            // 1. Get the list of category IDs the user has selected
            var userCategoryResponse = await SupabaseManager.Instance
                .From<UserCategory>()
                .Where(x => x.UserId == currentUserId)
                .Get();

            List<long> selectedCategoryIds = userCategoryResponse.Models
                .Select(x => x.CategoryId)
                .ToList();
            Debug.Log(selectedCategoryIds.Count);
            foreach(var cat in selectedCategoryIds)
                Debug.Log(cat);

            // 2. Fetch challenges that match the user's selected categories
            // We use the .Filter method to find challenges where fk_categoryid is IN our list
            var challengeResponse = await SupabaseManager.Instance
                .From<Challenge>()
                .Filter("fk_categoryid", Constants.Operator.In, selectedCategoryIds)
                .Get();

            List<Challenge> availableChallenges = challengeResponse.Models;
            foreach(var challenge in availableChallenges)
                Debug.Log(challenge.Description);

            // 3. Pick a random challenge from the list
            int randomIndex = UnityEngine.Random.Range(0, availableChallenges.Count);
            Challenge randomChallenge = availableChallenges[randomIndex];

            // 4. Create the new UserChallenge entry
            var newUserChallenge = new UserChallenge
            {
                UserId = currentUserId,
                ChallengeId = randomChallenge.Id,
                // Manual defaults that match Supabase defaults.
                Status = "Active",
                Date = DateTime.UtcNow.Date,
                TimeToComplete = DateTime.UtcNow.Date.AddDays(1) // Today + 1. 
            };
            Debug.Log(newUserChallenge.UserId);
            var insertResponse = await SupabaseManager.Instance
                .From<UserChallenge>()
                .Insert(newUserChallenge);

            Debug.Log($"Successfully assigned challenge: {randomChallenge.Description}");
            return insertResponse.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating user challenge: {e.Message}");
            return null;
        }
    }
    public async Task ClaimRewardAsync(UserChallenge uc)
    {
        // Update status in DB so it stays green forever
        uc.Status = "claimed";
        await SupabaseManager.Instance.From<UserChallenge>().Update(uc);
    }
    // 2. READ ALL (For Current User)
    public async Task<List<UserChallenge>> GetAllUserChallengesAsync()
    {
        try
        {
            Debug.Log("Fetching challenges for current user...");
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;

            // --- THE FIX IS HERE ---
            // We add .Select("*, Challenge(*)") to pull the template data too
            var response = await SupabaseManager.Instance.From<UserChallenge>()
                .Select("*, ChallengeData:fk_challengeid(*)")
                .Where(x => x.UserId == currentUserId)
                .Get();
            // -----------------------

            for (int i = 0; i < response.Models.Count; i++)
            {
                // Logic to check expiration
                bool isExpired = response.Models[i].TimeToComplete <= DateTime.UtcNow.Date;
                bool isActive = response.Models[i].Status == "Active";

                if (isExpired && isActive)
                {
                    response.Models[i] = await UpdateUserChallengeStatusAsync(response.Models[i].Id, "Failed");
                }
            }
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
        var challengeToUpdate = await GetUserChallengeByIdAsync(userChallengeId);

        if (challengeToUpdate == null) return null;

        challengeToUpdate.Status = newStatus;

        var response = await SupabaseManager.Instance.From<UserChallenge>().Update(challengeToUpdate);
        return response.Models[0];
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