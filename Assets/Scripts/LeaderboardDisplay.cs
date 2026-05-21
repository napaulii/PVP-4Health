using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LeaderboardDisplay : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Transform contentParent; // Drag your ScrollRect -> Content here
    [SerializeField] private GameObject fortressEntryPrefab; // Your UI prefab box row

    private LeaderboardController _leaderboardController;

    async void Start()
    {
        _leaderboardController = new LeaderboardController();
        await RefreshLeaderboard();
    }

    public async System.Threading.Tasks.Task RefreshLeaderboard()
    {
        // 1. Clear out any previous UI rows
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // 2. Fetch the matched leaderboard data
        List<LeaderboardController.LeaderboardEntry> leaderboard = await _leaderboardController.GetFortressLeaderboardAsync();

        // 3. Instantiate and populate UI elements
        int rank = 1;
        foreach (var entry in leaderboard)
        {
            GameObject rowElement = Instantiate(fortressEntryPrefab, contentParent);
            
            // Look for TextMeshPro components in the prefab children
            TMP_Text rankText = rowElement.transform.Find("RankText")?.GetComponent<TMP_Text>();
            TMP_Text nameText = rowElement.transform.Find("NameText")?.GetComponent<TMP_Text>();
            TMP_Text levelText = rowElement.transform.Find("LevelText")?.GetComponent<TMP_Text>();
            // TMP_Text stateText = rowElement.transform.Find("StateText")?.GetComponent<TMP_Text>();

            // Assign values safely
            if (rankText != null) rankText.text = $"#{rank}";
            if (nameText != null) nameText.text = entry.GroupTitle;
            if (levelText != null) levelText.text = $"LVL {entry.FortressData.Level}";
            // if (stateText != null) stateText.text = entry.FortressData.State;

            rank++;
        }
    }
}