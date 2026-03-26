using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class ChallengesScript : MonoBehaviour
{
    public static ChallengesScript Instance { get; private set; }

    [Header("Personal section")]
    [SerializeField] private Transform personalContainer;   // Bracket under PersonalChallWin
    [SerializeField] private Transform personalRowTemplate; // Button under PersonalChallWin

    [Header("Group section")]
    [SerializeField] private Transform groupContainer;      // Bracket under GroupChallWin
    [SerializeField] private Transform groupRowTemplate;    // Button under GroupChallWin

    [Header("Status label (optional)")]
    [SerializeField] private TextMeshProUGUI statusLabel;

    [Header("User info")]
    [SerializeField] private int userId = 1;
    [SerializeField] private int groupId = 1;

    // Runtime tracking
    private readonly Dictionary<int, bool> personalCompleted = new Dictionary<int, bool>();
    private readonly Dictionary<int, bool> groupCompleted = new Dictionary<int, bool>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (personalRowTemplate != null) personalRowTemplate.gameObject.SetActive(false);
        if (groupRowTemplate != null) groupRowTemplate.gameObject.SetActive(false);
    }

    // -----------------------------------------------------------------------
    // Public API — call this after receiving data from your backend
    // -----------------------------------------------------------------------

    public void LoadChallenges(List<UserChallengeData> personal, List<GroupChallengeData> group)
    {
        ClearRows(personalContainer, personalRowTemplate);
        ClearRows(groupContainer, groupRowTemplate);

        personalCompleted.Clear();
        groupCompleted.Clear();

        foreach (UserChallengeData uc in personal)
        {
            bool done = uc.status == "completed";
            personalCompleted[uc.id] = done;
            SpawnRow(personalContainer, personalRowTemplate,
                     uc.challenge?.description ?? $"Challenge {uc.fk_Challengeid}",
                     done,
                     () => OnPersonalCheckClicked(uc));
        }

        foreach (GroupChallengeData gc in group)
        {
            bool done = gc.status == "completed";
            groupCompleted[gc.id] = done;
            SpawnRow(groupContainer, groupRowTemplate,
                     gc.challenge?.description ?? $"Challenge {gc.fk_Challengeid}",
                     done,
                     () => OnGroupCheckClicked(gc));
        }

        RefreshStatusLabel();
    }

    // -----------------------------------------------------------------------
    // Row spawning
    // -----------------------------------------------------------------------

    private void SpawnRow(Transform container, Transform template,
                          string description, bool completed, Action onCheck)
    {
        if (container == null || template == null) return;

        Transform row = Instantiate(template, container);
        row.gameObject.SetActive(true);

        // Set description text — looks for a TMP on the row itself or a child named "ChallengeText"
        TextMeshProUGUI label = row.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.SetText(description);

        // Checkmark button
        Button btn = row.GetComponentInChildren<Button>();
        if (btn != null)
        {
            SetCheckVisual(btn, completed);
            btn.onClick.AddListener(() =>
            {
                onCheck?.Invoke();
            });
        }
    }

    private void ClearRows(Transform container, Transform template)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            if (child != template)
                Destroy(child.gameObject);
        }
    }

    // -----------------------------------------------------------------------
    // Check interactions
    // -----------------------------------------------------------------------

    private async void OnPersonalCheckClicked(UserChallengeData uc)
    {
        if (personalCompleted.TryGetValue(uc.id, out bool done) && done) return; // already done

        // Mark locally
        personalCompleted[uc.id] = true;
        RefreshStatusLabel();

        // Optionally reward coins
        if (uc.challenge != null)
            CoinManager.Instance?.AddCoins(uc.challenge.reward);

        // Tell backend
        if (FortressBuilder.Network.BackendAPI.Instance != null)
        {
            var response = await FortressBuilder.Network.BackendAPI.Instance
                .CompleteUserChallenge(userId, uc.fk_Challengeid);

            if (response != null && response.success)
                Debug.Log($"[Challenges] Personal challenge {uc.id} confirmed by server.");
            else
                Debug.LogWarning($"[Challenges] Server rejected personal challenge {uc.id}.");
        }
    }

    private async void OnGroupCheckClicked(GroupChallengeData gc)
    {
        if (groupCompleted.TryGetValue(gc.id, out bool done) && done) return;

        groupCompleted[gc.id] = true;
        RefreshStatusLabel();

        if (gc.challenge != null)
            CoinManager.Instance?.AddCoins(gc.challenge.reward);

        if (FortressBuilder.Network.BackendAPI.Instance != null)
        {
            var response = await FortressBuilder.Network.BackendAPI.Instance
                .CompleteGroupChallenge(userId, groupId, gc.fk_Challengeid);

            if (response != null && response.success)
                Debug.Log($"[Challenges] Group challenge {gc.id} confirmed by server.");
            else
                Debug.LogWarning($"[Challenges] Server rejected group challenge {gc.id}.");
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>Tints the checkmark button salmon (incomplete) or green (complete).</summary>
    private void SetCheckVisual(Button btn, bool completed)
    {
        Image img = btn.GetComponent<Image>();
        if (img != null)
            img.color = completed
                ? new Color(0.4f, 0.85f, 0.4f)   // green = done
                : new Color(0.96f, 0.5f, 0.4f);   // salmon = to-do (matches your prototype)
    }

    private void RefreshStatusLabel()
    {
        if (statusLabel == null) return;

        int total = personalCompleted.Count + groupCompleted.Count;
        int completed = 0;
        foreach (bool v in personalCompleted.Values) if (v) completed++;
        foreach (bool v in groupCompleted.Values) if (v) completed++;

        string status = completed == 0 ? "just started"
                      : completed < total / 2 ? "in progress"
                      : completed < total ? "almost there"
                      : "healthy";

        statusLabel.SetText("Status: " + status);
    }
}