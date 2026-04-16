using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;

public class AchievementList : MonoBehaviour
{
    public Transform content;
    public GameObject prefab;

    async void OnEnable()
    {
        var achievements = await LoadAchievements();
        await ApplyLogic(achievements);
        Populate(achievements);
    }

    async Task<List<Achievement>> LoadAchievements()
    {
        var response = await SupabaseManager.Instance
            .From<Achievement>()
            .Get();

        return response.Models;
    }

    async Task ApplyLogic(List<Achievement> list)
    {
        string userId = SupabaseManager.Instance.Auth.ToString();

        var challengeController = new ChallengeController();
        var userChallenges = await challengeController.GetUserChallengesAsync(userId);
        var groupChallenges = await challengeController.GetGroupChallengesAsync(1); 

        foreach (var a in list)
        {
            
            if (a.Title.ToLower().Contains("daily"))
            {
                a.Unlocked = userChallenges
                    .FindAll(c => c.Challenge.Type == "daily")
                    .TrueForAll(c => c.Status == "completed");
            }

            
            if (a.Title.ToLower().Contains("group"))
            {
                int completed = groupChallenges
                    .FindAll(g => g.Status == "completed").Count;

                a.Unlocked = completed >= 5;
            }
        }
    }

    void Populate(List<Achievement> list)
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (var a in list)
        {
            var obj = Instantiate(prefab, content);
            obj.GetComponent<AchievementItemUI>().Setup(a);
        }
    }
}