using UnityEngine;

public class ChallengesManager : MonoBehaviour
{
    [Header("Personal Challenges")]
    [SerializeField] private Transform personalContent;
    [SerializeField] private GameObject personalPrefab;

    [Header("Group Challenges")]
    [SerializeField] private Transform groupContent;
    [SerializeField] private GameObject groupPrefab;

    private void Start()
    {
        AddPersonal("Complete a 7-day streak", 200);
        AddPersonal("Log your first habit", 50);
        AddPersonal("Finish 3 habits in one day", 150);
        AddGroup("Group completes 10 habits", 300);
        AddGroup("All members log in today", 100);
    }

    public ChallengeItem AddPersonal(string title, int coinReward)
        => SpawnChallenge(personalPrefab, personalContent, title, coinReward);

    public ChallengeItem AddGroup(string title, int coinReward)
        => SpawnChallenge(groupPrefab, groupContent, title, coinReward);

    private ChallengeItem SpawnChallenge(GameObject prefab, Transform parent, string title, int coinReward)
    {
        if (prefab == null || parent == null)
        {
            Debug.LogWarning("ChallengesManager: prefab or parent not assigned.");
            return null;
        }

        var go = Instantiate(prefab, parent);
        var item = go.GetComponent<ChallengeItem>();
        item?.Setup(title, coinReward);
        return item;
    }
}