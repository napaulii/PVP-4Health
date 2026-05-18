using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;

public class AchievementDefinitionController
{
    // READ ALL
    public async Task<List<AchievementDefinition>> GetAllAchievementDefinitionsAsync()
    {
        try
        {
            var response = await SupabaseManager.Instance
                .From<AchievementDefinition>()
                .Get();

            return response.Models;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching achievement definitions: {e.Message}");
            return new List<AchievementDefinition>();
        }
    }

    // READ ONE
    public async Task<AchievementDefinition> GetAchievementDefinitionByIdAsync(long achievementId)
    {
        try
        {
            var response = await SupabaseManager.Instance
                .From<AchievementDefinition>()
                .Where(x => x.Id == achievementId)
                .Get();

            if (response.Models.Count > 0)
                return response.Models[0];

            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching achievement definition: {e.Message}");
            return null;
        }
    }
}