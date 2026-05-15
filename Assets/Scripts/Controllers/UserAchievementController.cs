using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;

public class UserAchievementController
{
    // CREATE
    public async Task<UserAchievement> CreateUserAchievementAsync(long achievementId)
    {
        try
        {
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;

            var newUserAchievement = new UserAchievement
            {
                UserId = currentUserId,
                AchievementId = achievementId,
                IsUnlocked = false,
                IsClaimed = false
            };

            var response = await SupabaseManager.Instance
                .From<UserAchievement>()
                .Insert(newUserAchievement);

            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating user achievement: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            return null;
        }
    }

    // READ ALL FOR CURRENT USER
    public async Task<List<UserAchievement>> GetAllUserAchievementsAsync()
    {
        try
        {
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;

            var response = await SupabaseManager.Instance
                .From<UserAchievement>()
                .Where(x => x.UserId == currentUserId)
                .Get();

            return response.Models;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching user achievements: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            return new List<UserAchievement>();
        }
    }

    // READ ONE
    public async Task<UserAchievement> GetUserAchievementByIdAsync(long id)
    {
        try
        {
            var response = await SupabaseManager.Instance
                .From<UserAchievement>()
                .Where(x => x.Id == id)
                .Get();

            if (response.Models.Count > 0)
                return response.Models[0];

            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching user achievement: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            return null;
        }
    }

    // UPDATE (full model)
    public async Task<UserAchievement> UpdateUserAchievementAsync(UserAchievement achievement)
    {
        try
        {
            var response = await SupabaseManager.Instance
                .From<UserAchievement>()
                .Update(achievement);

            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error updating user achievement: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            return null;
        }
    }

    // DELETE
    public async Task<bool> DeleteUserAchievementAsync(long id)
    {
        try
        {
            await SupabaseManager.Instance
                .From<UserAchievement>()
                .Where(x => x.Id == id)
                .Delete();

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deleting user achievement: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            return false;
        }
    }

    public async Task UnlockAchievementAsync(UserAchievement achievement)
    {
        try
        {
            achievement.IsUnlocked = true;

            await achievement.Update<UserAchievement>();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error unlocking achievement: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }

    public async Task ClaimAchievementAsync(UserAchievement achievement)
    {
        try
        {
            achievement.IsClaimed = true;

            await achievement.Update<UserAchievement>();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error claiming achievement: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }
    }
}