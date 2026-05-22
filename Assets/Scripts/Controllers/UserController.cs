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
                var user = response.Models[0];

                // Only write to the database if a new day has actually occurred
                if (user.LastTimeUpdatedCount.Date < DateTime.UtcNow.Date)
                {
                    user.DailyHabitCompletedCount = 0;
                    user.LastTimeUpdatedCount = DateTime.UtcNow.Date;

                    // Await the update so it completes safely before the method returns, 
                    // preventing background race conditions
                    await SupabaseManager.Instance.From<User>().Update(user);
                }

                return user;
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
    public async Task<int> GetTodayStepsAsync()
    {
        var currentUser = await GetCurrentUserAsync();
        if (currentUser == null)
        {
            Debug.LogError("[GetTodaySteps] Failed to fetch current user from Supabase.");
            return 0;
        }

        int currentDeviceSteps = HardwareStepCounter.Instance.GetTotalDeviceSteps();
        DateTime todayLocal = DateTime.Today;

        // DIAGNOSTIC LOG: Watch these values on your screen
        Debug.LogError($"[StepDebug] DeviceTotal: {currentDeviceSteps} | Baseline: {currentUser.StepBaseline} | " +
                       $"SavedDate: {currentUser.StepBaselineDate.Date:yyyy-MM-dd} | TodayLocal: {todayLocal:yyyy-MM-dd}");

        // If the saved date is older than today, reset the baseline for the new day
        if (currentUser.StepBaselineDate.Date < todayLocal)
        {
            Debug.LogError($"[StepDebug] New day detected! Resetting baseline to {currentDeviceSteps}.");

            currentUser.StepBaseline = currentDeviceSteps;

            // --- THE FIX ---
            // Force Unity to treat todayLocal as UTC so PostgreSQL does not perform timezone shifting
            currentUser.StepBaselineDate = DateTime.SpecifyKind(todayLocal, DateTimeKind.Utc);

            // Save the new baseline to Supabase
            await SupabaseManager.Instance.From<SupabaseModels.User>().Update(currentUser);
            return 0;
        }

        // Today's steps = (Current hardware total) - (Midnight baseline total)
        int todaySteps = currentDeviceSteps - currentUser.StepBaseline;

        // Handle device reboot edge-case
        if (todaySteps < 0)
        {
            Debug.LogWarning("[StepDebug] Device reboot detected. Resetting baseline.");
            currentUser.StepBaseline = 0;
            await SupabaseManager.Instance.From<SupabaseModels.User>().Update(currentUser);
            todaySteps = currentDeviceSteps;
        }

        Debug.LogError($"[StepDebug] Final Calculated Steps: {todaySteps}");
        return todaySteps;
    }
}