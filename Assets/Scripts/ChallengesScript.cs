using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SupabaseModels;

public class ChallengesScript : MonoBehaviour
{
    public static ChallengesScript Instance { get; private set; }

    [Header("Personal section")]
    [SerializeField] private Transform personalContainer;
    [SerializeField] private Transform personalRowTemplate;

    [Header("Group section")]
    [SerializeField] private Transform groupContainer;
    [SerializeField] private Transform groupRowTemplate;

    [Header("Status label (optional)")]
    [SerializeField] private TextMeshProUGUI statusLabel;

    [Header("User info")]
    [SerializeField] private string userId = "";
    [SerializeField] private long groupId = 1;

    private readonly Dictionary<long, bool> personalCompleted = new Dictionary<long, bool>();
    private readonly Dictionary<long, bool> groupCompleted = new Dictionary<long, bool>();

    // ADDED: Our new Database Controller
    private ChallengeController _challengeController;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Initialize the controller here
        _challengeController = new ChallengeController();
    }

    private void Start()
    {
        if (personalRowTemplate != null) personalRowTemplate.gameObject.SetActive(false);
        if (groupRowTemplate != null) groupRowTemplate.gameObject.SetActive(false);

        // Optional: You can auto-load challenges on start if you have a logged-in user!
        // if (!string.IsNullOrEmpty(SupabaseManager.Instance.Auth.CurrentUser?.Id)) {
        //     userId = SupabaseManager.Instance.Auth.CurrentUser.Id;
        //     FetchAndLoadChallenges();
        // }
    }

    // NEW METHOD: Easily fetch and load directly from Supabase
    public async void FetchAndLoadChallenges()
    {
        Debug.Log("Fetching challenges from Supabase...");
        List<UserChallenge> userChalls = await _challengeController.GetUserChallengesAsync(userId);
        List<GroupChallenge> groupChalls = await _challengeController.GetGroupChallengesAsync(groupId);

        LoadChallenges(userChalls, groupChalls);
    }

    public void LoadChallenges(List<UserChallenge> personal, List<GroupChallenge> group)
    {
        ClearRows(personalContainer, personalRowTemplate);
        ClearRows(groupContainer, groupRowTemplate);

        personalCompleted.Clear();
        groupCompleted.Clear();

        foreach (UserChallenge uc in personal)
        {
            bool done = uc.Status == "completed";
            personalCompleted[uc.Id] = done;
            string desc = uc.Challenge?.Description ?? $"Challenge {uc.ChallengeId}";
            SpawnRow(personalContainer, personalRowTemplate, desc, done, () => OnPersonalCheckClicked(uc));
        }

        foreach (GroupChallenge gc in group)
        {
            bool done = gc.Status == "completed";
            groupCompleted[gc.Id] = done;
            string desc = gc.Challenge?.Description ?? $"Group Challenge {gc.ChallengeId}";
            SpawnRow(groupContainer, groupRowTemplate, desc, done, () => OnGroupCheckClicked(gc));
        }

        RefreshStatusLabel();
    }

    // -----------------------------------------------------------------------
    // Row spawning logic (unchanged)
    // -----------------------------------------------------------------------
    private void SpawnRow(Transform container, Transform template, string description, bool completed, Action onCheck)
    {
        if (container == null || template == null) return;
        Transform row = Instantiate(template, container);
        row.gameObject.SetActive(true);
        TextMeshProUGUI label = row.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.SetText(description);
        Button btn = row.GetComponentInChildren<Button>();
        if (btn != null)
        {
            SetCheckVisual(btn, completed);
            btn.onClick.AddListener(() => { onCheck?.Invoke(); });
        }
    }

    private void ClearRows(Transform container, Transform template)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            if (child != template) Destroy(child.gameObject);
        }
    }

    // -----------------------------------------------------------------------
    // Check interactions - NOW USING SUPABASE CONTROLLER
    // -----------------------------------------------------------------------
    private async void OnPersonalCheckClicked(UserChallenge uc)
    {
        if (personalCompleted.TryGetValue(uc.Id, out bool done) && done) return;

        // Optimistically update UI
        personalCompleted[uc.Id] = true;
        RefreshStatusLabel();

        // 1. Give Coins (if CoinManager exists)
        if (uc.Challenge != null && CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(uc.Challenge.BalanceReward);
        }

        // 2. Tell Supabase using our new Controller!
        bool success = await _challengeController.CompleteUserChallengeAsync(uc);

        if (success)
        {
            Debug.Log($"[Challenges] Personal challenge {uc.Id} saved as completed in Supabase.");
        }
        else
        {
            Debug.LogWarning($"[Challenges] Failed to save completion to Supabase. Reverting UI.");
            // Revert UI if DB failed
            personalCompleted[uc.Id] = false;
            RefreshStatusLabel();
        }
    }

    private async void OnGroupCheckClicked(GroupChallenge gc)
    {
        if (groupCompleted.TryGetValue(gc.Id, out bool done) && done) return;

        groupCompleted[gc.Id] = true;
        RefreshStatusLabel();

        if (gc.Challenge != null && CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(gc.Challenge.BalanceReward);
        }

        // Tell Supabase using our new Controller!
        bool success = await _challengeController.CompleteGroupChallengeAsync(gc);

        if (success)
        {
            Debug.Log($"[Challenges] Group challenge {gc.Id} saved as completed in Supabase.");
        }
        else
        {
            Debug.LogWarning($"[Challenges] Failed to save group challenge to Supabase.");
            groupCompleted[gc.Id] = false;
            RefreshStatusLabel();
        }
    }

    // -----------------------------------------------------------------------
    // Helpers (unchanged)
    // -----------------------------------------------------------------------
    private void SetCheckVisual(Button btn, bool completed)
    {
        Image img = btn.GetComponent<Image>();
        if (img != null) img.color = completed ? new Color(0.4f, 0.85f, 0.4f) : new Color(0.96f, 0.5f, 0.4f);
    }

    private void RefreshStatusLabel()
    {
        if (statusLabel == null) return;
        int total = personalCompleted.Count + groupCompleted.Count;
        int completed = 0;
        foreach (bool v in personalCompleted.Values) if (v) completed++;
        foreach (bool v in groupCompleted.Values) if (v) completed++;
        string status = completed == 0 ? "just started" : completed < total / 2 ? "in progress" : completed < total ? "almost there" : "healthy";
        statusLabel.SetText("Status: " + status);
    }
}