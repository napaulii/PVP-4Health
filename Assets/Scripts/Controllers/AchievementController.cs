using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;

public class AchievementController
{
    // 1. CREATE
    public async Task<Achievement> CreateAchievementAsync(string title, string text, int xpReward, int balanceReward)
    {
        try
        {
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;

            var newAchievement = new Achievement
            {
                Title = title,
                Text = text,
                XpReward = xpReward,
                BalanceReward = balanceReward,
                UserId = currentUserId,
                Completed = false
            };

            var response = await SupabaseManager.Instance.From<Achievement>().Insert(newAchievement);
            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating achievement: {e.Message}");
            return null;
        }
    }

    // 2. READ ALL (For Current User)
    public async Task<List<Achievement>> GetAllAchievementsAsync()
    {
        try
        {
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;

            var response = await SupabaseManager.Instance.From<Achievement>()
                .Where(x => x.UserId == currentUserId)
                .Get();

            return response.Models;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching achievements: {e.Message}");
            return new List<Achievement>();
        }
    }

    // 3. READ ONE
    public async Task<Achievement> GetAchievementByIdAsync(long achievementId)
    {
        try
        {
            var response = await SupabaseManager.Instance.From<Achievement>()
                .Where(x => x.Id == achievementId)
                .Get();

            if (response.Models.Count > 0)
            {
                return response.Models[0];
            }
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching achievement by ID: {e.Message}");
            return null;
        }
    }

    public async Task<Achievement> CompleteAchievementAsync(long achievementId)
    {
        try
        {
            var achievementToUpdate = await GetAchievementByIdAsync(achievementId);

            if (achievementToUpdate == null)
            {
                Debug.LogWarning("Cannot complete: Achievement not found.");
                return null;
            }

            achievementToUpdate.Completed = true;

            var response = await SupabaseManager.Instance.From<Achievement>().Update(achievementToUpdate);
            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error completing achievement: {e.Message}");
            return null;
        }
    }
}