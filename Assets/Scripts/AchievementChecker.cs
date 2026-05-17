using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;
using UnityEngine.SocialPlatforms.Impl;

public class AchievementChecker : MonoBehaviour
{
    private AchievementDefinitionController definitionController;
    private UserAchievementController userAchievementController;

    public event Action<AchievementDefinition> OnAchievementUnlocked;

    private bool isChecking;

    public static AchievementChecker Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private async void Start()
    {
        definitionController = new AchievementDefinitionController();
        userAchievementController = new UserAchievementController();

        // Initial startup check
        await CheckAchievementsAsync();
    }


    public async Task CheckAchievementsAsync(Habit habit = null)
    {
        // Prevent overlapping checks
        if (isChecking)
        {
            Debug.Log("[Checker] Check already running — skipping.");
            return;
        }

        isChecking = true;

        try
        {
            Debug.Log($"[Checker] Running. CoinManager.Coins = {CoinManager.Instance?.Coins}");

            List<AchievementDefinition> definitions =
                await definitionController.GetAllAchievementDefinitionsAsync();

            Debug.Log($"[Checker] Loaded {definitions.Count} definitions");

            List<UserAchievement> userAchievements =
                await userAchievementController.GetAllUserAchievementsAsync();

            Debug.Log($"[Checker] Loaded {userAchievements.Count} user achievements");

            foreach (AchievementDefinition definition in definitions)
            {
                Debug.Log(
                    $"[Checker] Checking: '{definition.Title}' | Type: {definition.AchievementType} | Target: {definition.TargetValue}");

                UserAchievement userAchievement =
                    userAchievements.FirstOrDefault(x => x.AchievementId == definition.Id);

                // Create missing row
                if (userAchievement == null)
                {
                    Debug.Log($"[Checker] No row found for '{definition.Title}' — creating...");

                    userAchievement = await userAchievementController
                        .CreateUserAchievementAsync(definition.Id);

                    if (userAchievement == null)
                    {
                        Debug.LogError(
                            $"[Checker] Failed to create row for '{definition.Title}'");

                        continue;
                    }

                    Debug.Log(
                        $"[Checker] Created row. ID = {userAchievement.Id}");

                    // Add to local list to avoid duplicate creation later
                    userAchievements.Add(userAchievement);
                }

                // Skip already unlocked
                if (userAchievement.IsUnlocked)
                {
                    Debug.Log(
                        $"[Checker] '{definition.Title}' already unlocked — skip");

                    continue;
                }

                bool shouldUnlock = false;

                switch (definition.AchievementType)
                {
                    case "streak":

                        List<Habit> habits =
                            await new HabitController().GetAllHabitsAsync();

                        int longestStreak =
                            habits.Count > 0
                                ? habits.Max(h => h.LongestStreak)
                                : 0;

                        shouldUnlock =
                            longestStreak >= definition.TargetValue;

                        Debug.Log(
                            $"[Checker] Streak check: {longestStreak} >= {definition.TargetValue} = {shouldUnlock}");

                        break;

                    case "habits_completed":

                        List<Habit> completedHabits =
                            await new HabitController().GetAllHabitsAsync();

                        int totalCompleted =
                            completedHabits.Sum(h => GetTotalHabitsCompletedCount(h));

                        shouldUnlock =
                            totalCompleted >= definition.TargetValue;

                        Debug.Log(
                            $"[Checker] Completed habits check: {totalCompleted} >= {definition.TargetValue} = {shouldUnlock}");

                        break;

                    case "user_challenges_completed":

                        List<UserChallenge> userChallenges =
                            await new UserChallengeController().GetAllUserChallengesAsync();

                        

                        int completedUserChallenges =
                            userChallenges.Count(c => c.Status == "completed");

                        shouldUnlock =
                            completedUserChallenges >= definition.TargetValue;

                        Debug.Log(
                            $"[Checker] User challenges check: {completedUserChallenges} >= {definition.TargetValue} = {shouldUnlock}");

                        break;

                    /*case "group_challenges_completed":

                        List<GroupChallenge> groupChallenges =
                            await new GroupChallengeController().GetChallengesForGroupAsync();

                        int completedGroupChallenges = groupChallenges.Count(c => c.Status == "completed");

                        shouldUnlock =
                            completedGroupChallenges >= definition.TargetValue;

                        Debug.Log(
                            $"[Checker] Group challenges check: {completedGroupChallenges} >= {definition.TargetValue} = {shouldUnlock}");

                        break;*/

                    case "coins":

                        int currentCoins = await GetUserBalanceAsync();

                        shouldUnlock = currentCoins >= definition.TargetValue;

                        Debug.Log(
                            $"[Checker] Coins check: {currentCoins} >= {definition.TargetValue} = {shouldUnlock}");

                        break;

                    default:

                        Debug.LogWarning(
                            $"[Checker] Unknown type: {definition.AchievementType}");

                        break;
                }

                // Unlock achievement
                if (shouldUnlock)
                {
                    Debug.Log($"[Checker] Unlocking '{definition.Title}' (DB ID: {userAchievement.Id})...");

                    long achievementId = userAchievement.Id;

                    await userAchievementController.UnlockAchievementAsync(userAchievement);

                    // Verify unlock
                    var verifiedAchievement =
                        await userAchievementController.GetUserAchievementByIdAsync(achievementId);

                    if (verifiedAchievement != null && verifiedAchievement.IsUnlocked)
                    {
                        Debug.Log($"[Checker] Successfully unlocked '{definition.Title}' (DB ID: {achievementId})");

                        // Update local object too
                        userAchievement.IsUnlocked = true;

                        OnAchievementUnlocked?.Invoke(definition);
                    }
                    else
                    {
                        Debug.LogError($"[Checker] Failed to verify unlock for '{definition.Title}' (DB ID: {achievementId})");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Checker] EXCEPTION: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            isChecking = false;
        }
    }

    private async Task<int> GetUserBalanceAsync()
    {
        string userId = SupabaseManager.Instance.Auth.CurrentUser.Id;
        var response = await SupabaseManager.Instance
            .From<User>()
            .Where(x => x.Id == userId)
            .Get();

        return response.Models.Count > 0 ? response.Models[0].Balance : 0;
    }

    private int GetTotalHabitsCompletedCount(Habit habit)
    {
        if (habit.CompletionDataList == null) return 0;
        return habit.CompletionDataList.Count(x => x);
    }
}