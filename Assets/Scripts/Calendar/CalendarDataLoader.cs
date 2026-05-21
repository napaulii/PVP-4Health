using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SupabaseModels;
using UnityEngine;

public class CalendarDataLoader
{
    public HashSet<DateTime> CompletedDays { get; private set; } = new HashSet<DateTime>();

    private readonly HabitController habitController;
    private readonly UserChallengeController userChallengeController;

    public CalendarDataLoader()
    {
        habitController = new HabitController();
        userChallengeController = new UserChallengeController();
    }

    // ─── Auth ─────────────────────────────────────────────────────────────────
    // Note: Login is handled separately in the login scene
    // This method should only be called after user is already authenticated

    public async Task EnsureAuthenticated()
    {
        try
        {
            if (SupabaseManager.Instance?.Auth?.CurrentUser == null)
            {
                Debug.LogWarning("No user logged in. Please login from the login scene first.");
                await Task.CompletedTask;
            }
            else
            {
                Debug.Log($"Already authenticated as: {SupabaseManager.Instance.Auth.CurrentUser.Email}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Authentication check failed: {e.Message}");
        }
    }

    // ─── Load All Completed Days ──────────────────────────────────────────────

    public async Task LoadCompletedDays()
    {
        CompletedDays.Clear();

        if (SupabaseManager.Instance?.Auth?.CurrentUser == null)
        {
            Debug.LogWarning("Cannot load completed days: No user logged in");
            return;
        }

        try
        {
            var habits = await habitController.GetAllHabitsAsync();
            if (habits != null)
                foreach (var habit in habits)
                {
                    if (habit.CompletionDataList == null) continue;
                    for (int i = 0; i < habit.CompletionDataList.Count; i++)
                        if (habit.CompletionDataList[i])
                            CompletedDays.Add(habit.DateOfCreation.AddDays(i).Date);
                }
        }
        catch (Exception e) { Debug.LogError($"Habits error: {e.Message}"); }

        try
        {
            var challenges = await userChallengeController.GetAllUserChallengesAsync();
            if (challenges != null)
                foreach (var c in challenges)
                    if (c.CompletedDate.HasValue)
                        CompletedDays.Add(c.CompletedDate.Value.Date);
        }
        catch (Exception e) { Debug.LogError($"Challenges error: {e.Message}"); }

        Debug.Log($"Completed days loaded: {CompletedDays.Count}");
    }

    // ─── Get Habits For One Day ───────────────────────────────────────────────

    public async Task<List<Habit>> GetHabitsForDate(DateTime date)
    {
        var result = new List<Habit>();

        if (SupabaseManager.Instance?.Auth?.CurrentUser == null)
        {
            Debug.LogWarning("Cannot get habits: No user logged in");
            return result;
        }

        try
        {
            var habits = await habitController.GetAllHabitsAsync();
            if (habits == null) return result;

            foreach (var habit in habits)
            {
                if (habit.CompletionDataList == null) continue;
                int idx = (date.Date - habit.DateOfCreation.Date).Days;
                if (idx >= 0 && idx < habit.CompletionDataList.Count && habit.CompletionDataList[idx])
                    result.Add(habit);
            }
        }
        catch (Exception e) { Debug.LogError($"GetHabitsForDate error: {e.Message}"); }

        return result;
    }
}