using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;

public class UserController
{
    // 1. CREATE
    public async Task<SupabaseModels.User> CreateUserAsync(string nickname, string firstName, string lastName, string email, int groupid)
    {
        try
        {
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;

            // Added SupabaseModels. prefix here
            var newUser = new SupabaseModels.User
            {
                Id = currentUserId,
                Nickname = nickname,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Balance = 0,
                Xp = 0,
                GroupID = groupid
            };

            // Added SupabaseModels. prefix here
            var response = await SupabaseManager.Instance.From<SupabaseModels.User>().Insert(newUser);
            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating user: {e.Message}");
            return null;
        }
    }

    // 2. READ ALL 
    public async Task<List<SupabaseModels.User>> GetAllUsersAsync()
    {
        try
        {
            var response = await SupabaseManager.Instance.From<SupabaseModels.User>().Get();
            return response.Models;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching all users: {e.Message}");
            return new List<SupabaseModels.User>();
        }
    }

    // 3. READ ONE 
    public async Task<SupabaseModels.User> GetCurrentUserAsync()
    {
        try
        {
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;

            var response = await SupabaseManager.Instance.From<SupabaseModels.User>()
                .Where(x => x.Id == currentUserId)
                .Get();

            if (response.Models.Count > 0)
            {
                return response.Models[0];
            }
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching current user: {e.Message}");
            return null;
        }
    }

    // 4. UPDATE
    public async Task<SupabaseModels.User> UpdateUserAsync(int RewardBalance, int RewardXP, bool IsHabitCompleted)
    {
        Debug.Log("Updating User");
        try
        {
            var userToUpdate = await GetCurrentUserAsync();

            if (userToUpdate == null)
            {
                Debug.LogWarning("Cannot update: User not found.");
                return null;
            }
            if (IsHabitCompleted)
            {
                if (userToUpdate.DailyHabitCompletedCount >= 3)
                {
                    userToUpdate.DailyHabitCompletedCount++;
                    var response1 = await SupabaseManager.Instance.From<SupabaseModels.User>().Update(userToUpdate);
                    return response1.Models[0];
                }
            }
            userToUpdate.DailyHabitCompletedCount++;
            userToUpdate.Balance += RewardBalance;
            userToUpdate.Xp += RewardBalance;
            


            var response = await SupabaseManager.Instance.From<SupabaseModels.User>().Update(userToUpdate);
            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error updating user: {e.Message}");
            return null;
        }
    }
}