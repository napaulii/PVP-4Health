using UnityEngine;

public class AchievementsManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform contentParent;      // the "Content" object inside ScrollView
    [SerializeField] private GameObject achievementPrefab; // your Achievement prefab

    private void Start()
    {
        // Example: add some achievements at startup.
        // Remove or replace these with however you load your data.
        AddAchievement("Complete 7-day streak", coinReward: 200, xpReward: 100);
        AddAchievement("Log first habit", coinReward: 50, xpReward: 25);
        AddAchievement("Finish 3 challenges", coinReward: 150, xpReward: 75);
    }

    /// <summary>
    /// Instantiates a new achievement block and wires up its values.
    /// Call this from anywhere — e.g. when loading from a database.
    /// </summary>
    public AchievementItem AddAchievement(string title, int coinReward, int xpReward)
    {
        if (achievementPrefab == null || contentParent == null)
        {
            Debug.LogWarning("AchievementsManager: prefab or contentParent not assigned.");
            return null;
        }

        GameObject go = Instantiate(achievementPrefab, contentParent);
        AchievementItem item = go.GetComponent<AchievementItem>();

        if (item != null)
        {
            item.coinReward = coinReward;
            item.xpReward = xpReward;
            item.SetTitle(title);
        }

        return item;
    }
}