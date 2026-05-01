using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;

public class FortressController
{
    // 1. CREATE
    public async Task<Fortress> CreateFortressAsync(long groupId)
    {
        try
        {
            var newFortress = new Fortress
            {
                State = "Intact",
                Level = 1,
                GroupId = groupId
            };

            var response = await SupabaseManager.Instance.From<Fortress>().Insert(newFortress);
            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating fortress: {e.Message}");
            return null;
        }
    }

    // 2. READ (By Group ID)
    public async Task<Fortress> GetFortressByGroupIdAsync(long groupId)
    {
        try
        {
            var response = await SupabaseManager.Instance.From<Fortress>()
                .Where(x => x.GroupId == groupId)
                .Get();

            if (response.Models.Count > 0)
            {
                return response.Models[0];
            }
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching fortress by group ID: {e.Message}");
            return null;
        }
    }

    // 3. UPDATE
    public async Task<Fortress> UpdateFortressLevelAsync(long fortressId, int newLevel, string newState)
    {
        try
        {
            var response = await SupabaseManager.Instance.From<Fortress>()
                .Where(x => x.Id == fortressId)
                .Get();

            if (response.Models.Count == 0)
            {
                Debug.LogWarning("Cannot update: Fortress not found.");
                return null;
            }

            var fortressToUpdate = response.Models[0];
            fortressToUpdate.Level = newLevel;
            fortressToUpdate.State = newState;

            var updateResponse = await SupabaseManager.Instance.From<Fortress>().Update(fortressToUpdate);
            return updateResponse.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error updating fortress: {e.Message}");
            return null;
        }
    }

    // 4. DELETE
    public async Task<bool> DeleteFortressAsync(long fortressId)
    {
        try
        {
            await SupabaseManager.Instance.From<Fortress>()
                .Where(x => x.Id == fortressId)
                .Delete();

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deleting fortress: {e.Message}");
            return false;
        }
    }
}