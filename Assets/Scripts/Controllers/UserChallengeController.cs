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
    public async Task<UserChallenge> CreateUserChallengeAsync()
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


            // 2. Fetch challenges that match the user's selected categories
            // We use the .Filter method to find challenges where fk_categoryid is IN our list
            var challengeResponse = await SupabaseManager.Instance
                .From<Challenge>()
                .Filter("fk_categoryid", Constants.Operator.In, selectedCategoryIds)
                .Get();

            List<Challenge> availableChallenges = challengeResponse.Models;

            if (availableChallenges.Count == 0)
            {
                Debug.LogWarning("No challenges found for the selected categories.");
                return null;
            }

            // 3. Pick a random challenge from the list
            int randomIndex = UnityEngine.Random.Range(0, availableChallenges.Count);
            Challenge randomChallenge = availableChallenges[randomIndex];

            // 4. Create the new UserChallenge entry
            var newUserChallenge = new UserChallenge
            {
                UserId = currentUserId,
                ChallengeId = randomChallenge.Id,
            };

            var insertResponse = await SupabaseManager.Instance
                .From<UserChallenge>()
                .Insert(newUserChallenge);

            Debug.Log($"Successfully assigned challenge: {randomChallenge.Description}");
            return insertResponse.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error assigning random challenge: {e.Message}");
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