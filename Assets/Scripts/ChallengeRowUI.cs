using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using SupabaseModels;

public class ChallengeRowUI : MonoBehaviour
{
    [Header("Text")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI rewardText;
    public TextMeshProUGUI coinsText;

    [Header("Done Button")]
    public Button doneButton;

    [Header("Extra (shown for meal/traveler/step details)")]
    public TextMeshProUGUI extraText1;  // progress or destination
    public TextMeshProUGUI extraText2;  // distance
    public Button extraButton1;         // Take Photo / Check Distance / Generate
    public Button extraButton2;         // Open Map

    [Header("Colors")]
    public Color incompleteColor = Color.white;
    public Color completedColor = Color.green;
    public Color claimedColor = Color.gray;

    private UserChallenge _data;
    private ChallengeActions _actions;
    private ChallengeUIManager _uiManager;

    private bool isExpanded = false;

    //  Properties ChallengeActions writes to 
    public TextMeshProUGUI targetDestinationText => extraText1;
    public TextMeshProUGUI targetDestinationDistance => extraText2;
    public Button checkLocationButton => extraButton1;

    public void CloseDetails() { }

    //Setup

    public void Setup(UserChallenge data, ChallengeActions actions, ChallengeUIManager uiManager)
    {
        _data = data;
        _actions = actions;
        _uiManager = uiManager;

        if (titleText != null) titleText.text = data.ChallengeData.Description;
        if (rewardText != null) rewardText.text =
            $"+{data.ChallengeData.XpReward}";
        if (coinsText != null) coinsText.text = $"+{data.ChallengeData.BalanceReward}";

        // Hide extras by default
        SetExtra(false);

        string status = (data.Status ?? "active").ToLower();
        string challengeType = (data.ChallengeData.Type ?? "").ToLower();

        bool isMeal = challengeType.Contains("meal");
        bool isTraveler = challengeType.Contains("traveler");

        switch (status)
        {
            case "claimed":
                SetDoneButton(claimedColor, "Claimed", false);
                break;

            case "completed":
                SetDoneButton(completedColor, "Claim", true);
                doneButton.onClick.RemoveAllListeners();
                doneButton.onClick.AddListener(OnClaimRewardPressed);
                break;

            default:
                SetDoneButton(incompleteColor, "Done", false);
                SetExtra(false);   // keep collapsed initially
                break;
        }
    }

    //  Traveler 

    void SetupTraveler(UserChallenge data)
    {
        ShowExtraText1(string.IsNullOrEmpty(data.TargetName)
            ? "GPS warming up..." : $"Target: {data.TargetName}");

        if (extraText2 != null) extraText2.gameObject.SetActive(true);

        string btnLabel = string.IsNullOrEmpty(data.TargetName)
            ? "Generate Location" : "Check Distance";
        ShowExtraButton1(btnLabel, () => _actions.ExecuteChallengeAction(_data, this));

        if (data.TargetLatitude.HasValue && data.TargetLongitude.HasValue)
        {
            double lat = data.TargetLatitude.Value;
            double lng = data.TargetLongitude.Value;
            ShowExtraButton2("Open Map", () => _actions.OpenMapForTarget(lat, lng));
        }

        if (string.IsNullOrEmpty(data.TargetName))
            _actions.AutoGenerateTravelerLocation(data, this);
    }

    // Step 

    async void UpdateStepProgress(UserChallenge data)
    {
        int steps = await new UserController().GetTodayStepsAsync();
        int target = ExtractTarget(data.ChallengeData.Description);

        if (extraText1 != null) extraText1.text = $"{steps} / {target} steps";

        if (steps >= target && data.Status.ToLower() == "active")
            await MarkStepCompleted(data);
    }

    public void ToggleExpand()
    {
        isExpanded = !isExpanded;

        if (isExpanded)
            ShowDetails();
        else
            SetExtra(false);
    }

    void ShowDetails()
    {
        string challengeType = (_data.ChallengeData.Type ?? "").ToLower();

        if (challengeType.Contains("meal"))
        {
            ShowExtraButton1("Take Photo",
                () => _actions.ExecuteChallengeAction(_data, this));
        }
        else if (challengeType.Contains("traveler"))
        {
            SetupTraveler(_data);
        }
        else
        {
            ShowExtraText1("Reading steps...");
            UpdateStepProgress(_data);
        }
    }

    int ExtractTarget(string desc)
    {
        string n = "";
        foreach (char c in desc) if (char.IsDigit(c)) n += c;
        return int.TryParse(n, out int t) ? t : 5000;
    }

    async Task MarkStepCompleted(UserChallenge data)
    {
        data.Status = "completed";
        await new UserChallengeController().UpdateUserChallengeStatusAsync(data.Id, "completed");
        _uiManager.RefreshUI();
    }

    //  Claim

    public async void OnClaimRewardPressed()
    {
        doneButton.interactable = false;
        await new UserController().UpdateUserAsync(
            _data.ChallengeData.BalanceReward,
            _data.ChallengeData.XpReward, false);
        await new UserChallengeController()
            .UpdateUserChallengeStatusAsync(_data.Id, "claimed");
        _data.Status = "claimed";
        if (CoinManager.Instance != null)
            await CoinManager.Instance.RefreshBalanceFromServer();
        _uiManager.RefreshUI();
        Object.FindFirstObjectByType<FortressUpdateScript>()?.UpdateFortressModelAsync();
    }

    // Helpers

    void SetExtra(bool active)
    {
        if (extraText1 != null) extraText1.gameObject.SetActive(active);
        if (extraText2 != null) extraText2.gameObject.SetActive(active);
        if (extraButton1 != null) extraButton1.gameObject.SetActive(active);
        if (extraButton2 != null) extraButton2.gameObject.SetActive(active);
    }

    void SetDoneButton(Color color, string label, bool interactable)
    {
        if (doneButton == null) return;
        doneButton.interactable = interactable;
        var img = doneButton.GetComponent<Image>();
        if (img != null) img.color = color;
        var tmp = doneButton.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = label;
    }

    void ShowExtraText1(string text)
    {
        if (extraText1 == null) return;
        extraText1.gameObject.SetActive(true);
        extraText1.text = text;
    }

    void ShowExtraButton1(string label, UnityEngine.Events.UnityAction action)
    {
        if (extraButton1 == null) return;
        extraButton1.gameObject.SetActive(true);
        extraButton1.onClick.RemoveAllListeners();
        extraButton1.onClick.AddListener(action);
        var tmp = extraButton1.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = label;
    }

    void ShowExtraButton2(string label, UnityEngine.Events.UnityAction action)
    {
        if (extraButton2 == null) return;
        extraButton2.gameObject.SetActive(true);
        extraButton2.onClick.RemoveAllListeners();
        extraButton2.onClick.AddListener(action);
        var tmp = extraButton2.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = label;
    }

    public void SetEmpty()
    {
        if (titleText != null) titleText.text = "No challenge available";
        if (rewardText != null) rewardText.text = "";
        if (coinsText != null) coinsText.text = "";
        if (doneButton != null) doneButton.interactable = false;
        SetExtra(false);
    }
}