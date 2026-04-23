using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;
using System.Linq;

public class HabitController
{
    // 1. CREATE
    public async Task<Habit> CreateHabitAsync(string title, string description)
    {
        try
        {
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;

            var newHabit = new Habit
            {
                Title = title,
                Description = description,
                UserId = currentUserId,
                CurrentStreak = 0,
                DateOfCreation = DateTime.UtcNow.Date,
                LongestStreak = 0,
                IsCompletedToday = false,
                CompletionDataList = new List<bool>()
            };

            var response = await SupabaseManager.Instance.From<Habit>().Insert(newHabit);
            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating habit: {e.Message}");
            return null;
        }
    }

    // 2. READ ALL 
    public async Task<List<Habit>> GetAllHabitsAsync()
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

    // 3. READ ONE 
    public async Task<Habit> GetHabitByIdAsync(long habitId)
    {
        try
        {
            var response = await SupabaseManager.Instance.From<Habit>()
                .Where(x => x.Id == habitId)
                .Get();

            if (response.Models.Count > 0)
            {
                return response.Models[0];
            }
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching habit by ID: {e.Message}");
            return null;
        }
    }

    // 4. UPDATE
    public async Task<Habit> UpdateHabitAsync(long habitId, string newTitle, string newDescription)
    {
        try
        {
            var habitToUpdate = await GetHabitByIdAsync(habitId);

            if (habitToUpdate == null)
            {
                Debug.LogWarning("Cannot update: Habit not found.");
                return null;
            }

            habitToUpdate.Title = newTitle;
            habitToUpdate.Description = newDescription;

            var response = await SupabaseManager.Instance.From<Habit>().Update(habitToUpdate);
            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error updating habit: {e.Message}");
            return null;
        }
    }

    // 5. DELETE
    public async Task<bool> DeleteHabitAsync(long habitId)
    {
        try
        {
            await SupabaseManager.Instance.From<Habit>()
                .Where(x => x.Id == habitId)
                .Delete();

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deleting habit: {e.Message}");
            return false;
        }
    }
    /// <summary>
    /// Marks Habit as completed, updates completion history based on missed days, updates streak counts
    /// </summary>
    /// <param name="habitId"></param>
    /// <returns></returns>
    public async Task ToggleCompletionAsync(long habitId)
    {
        var habit = await GetHabitByIdAsync(habitId);
        if (habit == null) return;

        if (!habit.IsCompletedToday)
        {   
            if(habit.CompletionDataList.Count == 0)
            {
            }
            else
            {
                TimeSpan dateDifference = DateTime.UtcNow.Date.Subtract(habit.LastTimeUpdatedCompletionList);
                int missedDayCount = dateDifference.Days;
                for(int i = 0; i < missedDayCount - 1; i++) 
                    habit.CompletionDataList.Add(false);
            }
            habit.CompletionDataList.Add(true);
            habit.LastTimeUpdatedCompletionList = DateTime.UtcNow.Date;
            habit.IsCompletedToday = true;
            habit.CurrentStreak++;
            if (habit.CurrentStreak > habit.LongestStreak)
                habit.LongestStreak = habit.CurrentStreak;
            await SupabaseManager.Instance.From<Habit>().Update(habit);
        }
    }
}