using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using SupabaseModels;

public class GroupChallengeRowUI : MonoBehaviour
{
    [Header("Text")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI rewardText;
    public TextMeshProUGUI coinsText;

    [Header("Done Button")]
    public Button doneButton;

    [Header("Extra")]
    public TextMeshProUGUI extraText1;
    public TextMeshProUGUI extraText2;
    public Button extraButton1;
    public Button extraButton2;

    [Header("Colors")]
    public Color incompleteColor = Color.white;
    public Color completedColor = Color.green;
    public Color claimedColor = Color.gray;

    public TextMeshProUGUI targetDestinationText => extraText1;
    public TextMeshProUGUI targetDestinationDistance => extraText2;
    public Button checkLocationButton => extraButton1;

    private GroupChallenge _data;
    private ChallengeActions _actions;
    public ChallengeUIManager _uiManager;

    private int _rewardXp = 500;
    private int _rewardCoins = 500;

    private bool isExpanded = false;

    public void Setup(GroupChallenge data, ChallengeActions actions, ChallengeUIManager uiManager)
    {
        _data = data;
        _actions = actions;
        _uiManager = uiManager;

        if (titleText != null) titleText.text = data.TargetName;
        if (rewardText != null) rewardText.text = $"+{_rewardXp}";
        if (coinsText != null) coinsText.text = $"+{_rewardCoins}";

        SetExtra(false);

        bool isStep = data.StepTarget.HasValue && data.StepTarget.Value > 0;
        string status = (data.Status ?? "active").ToLower();

        if (isStep)
            ShowExtra1($"{data.StepProgress} / {data.StepTarget.Value} steps");
        else
        {
            ShowExtra1($"{data.TravelsCompleted} / 20 locations visited");
            if (status == "active") SetupTraveler(data);
        }

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

    void SetupTraveler(GroupChallenge data)
    {
        ShowExtra1(string.IsNullOrEmpty(data.TargetName)
            ? "GPS warming up..." : $"Target: {data.TargetName}");

        if (extraText2 != null) extraText2.gameObject.SetActive(true);

        if (extraButton1 != null)
        {
            extraButton1.gameObject.SetActive(true);
            extraButton1.onClick.RemoveAllListeners();
            extraButton1.onClick.AddListener(OnCheckLocationClicked);
            var tmp = extraButton1.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = string.IsNullOrEmpty(data.TargetName)
                ? "Generate Location" : "Check Distance";
        }

        if (extraButton2 != null)
        {
            if (data.TargetLatitude.HasValue && data.TargetLongitude.HasValue)
            {
                double lat = data.TargetLatitude.Value;
                double lng = data.TargetLongitude.Value;
                extraButton2.gameObject.SetActive(true);
                extraButton2.onClick.RemoveAllListeners();
                extraButton2.onClick.AddListener(() => _actions.OpenMapForTarget(lat, lng));
            }
            else
            {
                extraButton2.gameObject.SetActive(false);
            }
        }

        if (string.IsNullOrEmpty(data.TargetName))
            _actions.AutoGenerateGroupTravelerLocation(data, this);
    }

    public async Task SyncAndRefreshSteps()
    {
        if (extraText1 != null) extraText1.text = "Syncing steps...";

        int currentSteps = await new UserController().GetTodayStepsAsync();
        string prefsKey = "GroupStepsSynced_" + _data.Id;
        int lastSynced = PlayerPrefs.GetInt(prefsKey, 0);
        int delta = currentSteps - lastSynced;

        if (delta > 0)
        {
            await new GroupChallengeController().SyncStepsToGroupAsync(_data.GroupId, delta);
            PlayerPrefs.SetInt(prefsKey, currentSteps);
            PlayerPrefs.Save();
        }

        var response = await SupabaseManager.Instance.From<GroupChallenge>()
            .Where(x => x.Id == _data.Id).Get();

        if (response.Models.Count > 0)
        {
            _data = response.Models[0];
            if (extraText1 != null)
                extraText1.text = $"{_data.StepProgress} / {_data.StepTarget.Value} steps";
            if (_data.StepProgress >= _data.StepTarget.Value && _data.Status == "Active")
                _uiManager.RefreshUI();
        }
    }

    public void OnCheckLocationClicked()
    {
        if (_actions != null && _data != null)
            _actions.ExecuteGroupTravelerChallenge(_data, this);
    }

    public async void OnClaimRewardPressed()
    {
        doneButton.interactable = false;
        await new UserController().UpdateUserAsync(_rewardCoins, _rewardXp, false);
        _data.Status = "claimed";
        await SupabaseManager.Instance.From<GroupChallenge>().Update(_data);
        if (CoinManager.Instance != null)
            await CoinManager.Instance.RefreshBalanceFromServer();
        _uiManager.RefreshUI();
        Object.FindFirstObjectByType<FortressUpdateScript>()?.UpdateFortressModelAsync();
    }

    public void CloseDetails() { }



    void ShowExtra1(string text)
    {
        if (extraText1 == null) return;
        extraText1.gameObject.SetActive(true);
        extraText1.text = text;
    }

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

    public void SetEmpty()
    {
        if (titleText != null) titleText.text = "No challenge available";
        if (rewardText != null) rewardText.text = "";
        if (coinsText != null) coinsText.text = "";
        if (doneButton != null) doneButton.interactable = false;
        SetExtra(false);
    }
}