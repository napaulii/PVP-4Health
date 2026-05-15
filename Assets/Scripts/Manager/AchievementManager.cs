using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;

public class AchievementsManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject achievementPrefab;

    private UserAchievementController userAchievementController;
    private AchievementDefinitionController definitionController;
    private HashSet<long> createdAchievements = new();
    private Dictionary<long, AchievementItem> achievementItems = new();


    private void Start()
    {
        userAchievementController = new UserAchievementController();
        definitionController = new AchievementDefinitionController();

        if (AchievementChecker.Instance != null)
        {
            AchievementChecker.Instance.OnAchievementUnlocked += OnAchievementUnlocked;
        }

        _ = LoadAchievements();
    }

    private void OnDestroy()
    {
        if (AchievementChecker.Instance != null)
        {
            AchievementChecker.Instance.OnAchievementUnlocked -= OnAchievementUnlocked;
        }
    }

    private async Task LoadAchievements()
    {
        List<UserAchievement> userAchievements = await userAchievementController.GetAllUserAchievementsAsync();
        List<AchievementDefinition> definitions = await definitionController.GetAllAchievementDefinitionsAsync();

        foreach (var userAch in userAchievements)
        {
            AchievementDefinition def = definitions.Find(d => d.Id == userAch.AchievementId);
            if (def == null) continue;
            CreateItem(def, userAch);
        }
    }

    public async void OnAchievementUnlocked(AchievementDefinition def)
    {
        List<UserAchievement> all =
            await userAchievementController.GetAllUserAchievementsAsync();

        UserAchievement updatedAchievement =
            all.Find(a => a.AchievementId == def.Id);

        if (updatedAchievement == null)
            return;

        // Update existing UI item
        if (achievementItems.TryGetValue(def.Id, out AchievementItem item))
        {
            item.Refresh(updatedAchievement);
        }
    }

    private void CreateItem(AchievementDefinition def, UserAchievement userAch)
    {
        if (achievementItems.ContainsKey(userAch.AchievementId))
            return;

        GameObject go = Instantiate(achievementPrefab, contentParent);

        AchievementItem item = go.GetComponent<AchievementItem>();

        if (item != null)
        {
            item.Initialize(def, userAch);

            achievementItems.Add(userAch.AchievementId, item);
        }
    }
}