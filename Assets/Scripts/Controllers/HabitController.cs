using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;
using System.Collections.Generic;

public class HabitController
{
    // CREATE
    public async Task<Habit> CreateHabitAsync(string title, string description)
    {
        try
        {
            // Get the currently logged-in user's ID
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;

            var newHabit = new Habit
            {
                Title = title,
                Description = description,
                UserId = currentUserId,
                CurrentStreak = 0,
                DateOfCreation = DateTime.UtcNow.Date, // Sets to today's date
                LongestStreak = 0,
                IsCompletedToday = false,
                CompletionDataList = new List<bool>()
            };

            var response = await SupabaseManager.Instance.From<Habit>().Insert(newHabit);

            // Supabase returns the created object, we return it to the game
            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating habit: {e.Message}");
            return null;
        }
    }

    // READ (Get all habits for the current user)
    public async Task<List<Habit>> GetUserHabitsAsync()
    {
        try
        {
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;

            var response = await SupabaseManager.Instance.From<Habit>()
                .Where(x => x.UserId == currentUserId)
                .Get();

            return response.Models;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching habits: {e.Message}");
            return new List<Habit>();
        }
    }

    // UPDATE
    public async Task<Habit> UpdateHabitAsync(Habit habitToUpdate)
    {
        try
        {
            // Supabase uses the Primary Key (Id) inside the model to know which row to update
            var response = await SupabaseManager.Instance.From<Habit>().Update(habitToUpdate);
            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error updating habit: {e.Message}");
            return null;
        }
    }

    // DELETE
    public async Task<bool> DeleteHabitAsync(Habit habitToDelete)
    {
        try
        {
            await SupabaseManager.Instance.From<Habit>().Delete(habitToDelete);
            return true; // Successfully deleted
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deleting habit: {e.Message}");
            return false;
        }
    }
}