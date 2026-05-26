using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;
using Postgrest;
using System.Linq;

public class UserChallengeController
{
    // 1. CREATE THREE UNIQUE DAILY CHALLENGES (Bulk Insert)
    public async Task CreateThreeUniqueChallengesAsync()
    {
        try
        {
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;

            // 1. Get the list of category IDs the user has selected
            var userCategoryResponse = await SupabaseManager.Instance
                .From<UserCategory>()
                .Where(x => x.UserId == currentUserId)
                .Get();

            List<long> selectedCategoryIds = userCategoryResponse.Models
                .Select(x => x.CategoryId)
                .ToList();

            if (selectedCategoryIds.Count == 0)
            {
                Debug.LogWarning("[UserChallengeController] User has not selected any categories yet.");
                return;
            }

            // 2. Fetch challenges that match the user's selected categories
            var challengeResponse = await SupabaseManager.Instance
                .From<Challenge>()
                .Filter("fk_categoryid", Constants.Operator.In, selectedCategoryIds)
                .Get();

            List<Challenge> availableChallenges = challengeResponse.Models;

            if (availableChallenges.Count == 0)
            {
                Debug.LogWarning("[UserChallengeController] No challenge templates found matching selected categories.");
                return;
            }

            // 3. Shuffle the template list to guarantee randomness and uniqueness
            System.Random rnd = new System.Random();
            int n = availableChallenges.Count;
            while (n > 1)
            {
                n--;
                int k = rnd.Next(n + 1);
                Challenge value = availableChallenges[k];
                availableChallenges[k] = availableChallenges[n];
                availableChallenges[n] = value;
            }

            // Pick up to 3 unique templates
            List<Challenge> chosenTemplates = new List<Challenge>();
            int limit = Mathf.Min(3, availableChallenges.Count);
            for (int i = 0; i < limit; i++)
            {
                chosenTemplates.Add(availableChallenges[i]);
            }

            // 4. Create the new UserChallenge models
            List<UserChallenge> inserts = new List<UserChallenge>();
            DateTime today = DateTime.UtcNow.Date;
            DateTime tomorrow = today.AddDays(1);

            foreach (var template in chosenTemplates)
            {
                inserts.Add(new UserChallenge
                {
                    UserId = currentUserId,
                    ChallengeId = template.Id,
                    Status = "Active",
                    Date = today,
                    TimeToComplete = tomorrow,
                    TargetLongitude = null,
                    TargetLatitude = null,
                    TargetName = null,
                });
            }

            // 5. Bulk insert all 3 challenges in a single database network call
            if (inserts.Count > 0)
            {
                await SupabaseManager.Instance
                    .From<UserChallenge>()
                    .Insert(inserts);
                Debug.Log($"[UserChallengeController] Generated {inserts.Count} new daily challenges.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[UserChallengeController] Error generating unique daily challenges: {e.Message}");
        }
    }

    public async Task ClaimRewardAsync(UserChallenge uc)
    {
        uc.Status = "claimed";
        await SupabaseManager.Instance.From<UserChallenge>().Update(uc);
    }

    // 2. READ ALL (With automatic cleanup and 3-challenge generation)
    public async Task<List<UserChallenge>> GetAllUserChallengesAsync()
    {
        try
        {
            Debug.Log("Fetching challenges for current user...");
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;
            DateTime todayUtc = DateTime.UtcNow.Date;

            // Fetch all user challenges (both active and expired history)
            var response = await SupabaseManager.Instance.From<UserChallenge>()
                .Select("*, ChallengeData:fk_challengeid(*)")
                .Where(x => x.UserId == currentUserId)
                .Get();

            List<UserChallenge> validChallenges = new List<UserChallenge>();

            // Iterate and identify expired items
            for (int i = 0; i < response.Models.Count; i++)
            {
                bool isExpired = response.Models[i].TimeToComplete <= todayUtc;
                bool isActive = response.Models[i].Status == "Active" || response.Models[i].Status == "completed";

                if (isExpired && isActive)
                {
                    // Update database record to Failed on the server
                    response.Models[i] = await UpdateUserChallengeStatusAsync(response.Models[i].Id, "Failed");
                }
                else if (!isExpired)
                {
                    // Keep non-expired challenges (Active, Completed, or Claimed)
                    validChallenges.Add(response.Models[i]);
                }
            }

            // IF THERE ARE LESS THAN 3 ACTIVE/VALID CHALLENGES, GENERATE 3 NEW ONES
            if (validChallenges.Count < 3)
            {
                Debug.Log($"[UserChallengeController] Found {validChallenges.Count} valid challenges. Generating 3 new ones...");
                await CreateThreeUniqueChallengesAsync();

                // Re-fetch only the newly created valid set
                var newResponse = await SupabaseManager.Instance.From<UserChallenge>()
                    .Select("*, ChallengeData:fk_challengeid(*)")
                    .Where(x => x.UserId == currentUserId)
                    .Where(x => x.TimeToComplete > todayUtc)
                    .Get();

                return newResponse.Models;
            }

            return validChallenges;
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