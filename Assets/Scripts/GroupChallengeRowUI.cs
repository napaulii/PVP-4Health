using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using SupabaseModels;

public class GroupChallengeRowUI : MonoBehaviour
{
    [Header("Colors")]
    public Color claimableColor;
    public Color incompleteColor;
    public Color claimedColor;

    [Header("Header References")]
    public TextMeshProUGUI descriptionText;
    public Button claimButton;
    public Image statusIcon;
    public Image buttonBackgroundImage;

    [Header("Expandable Area")]
    public GameObject detailsArea;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI progressText;

    [Header("Action Area")]
    public GameObject travelerActionGroup;
    public TextMeshProUGUI targetDestinationText;
    public TextMeshProUGUI targetDestinationDistance;
    public Button checkLocationButton;
    public Button openMapButton;

    private GroupChallenge _data;
    private ChallengeActions _actions;
    public GroupChallengeUIManager _uiManager;

    private int _rewardXp = 500;
    private int _rewardCoins = 500;

    public GroupChallengeUIManager uiManagerPublic => _uiManager;

    public void Setup(GroupChallenge data, ChallengeActions actions, GroupChallengeUIManager uiManager)
    {
        _data = data;
        _actions = actions;
        _uiManager = uiManager;

        descriptionText.text = data.TargetName;
        xpText.text = $"+{_rewardXp} XP";
        coinsText.text = $"+{_rewardCoins} Coins";
        statusIcon.color = Color.white;
        statusIcon.material = null;

        bool isStepChallenge = data.StepTarget.HasValue && data.StepTarget.Value > 0;

        if (isStepChallenge)
        {
            progressText.text = $"{data.StepProgress} / {data.StepTarget.Value} steps";
            if (travelerActionGroup != null) travelerActionGroup.SetActive(false);
            progressText.gameObject.SetActive(true);
        }
        else
        {
            progressText.text = $"{data.TravelsCompleted} / 20 locations visited";
            progressText.gameObject.SetActive(true);

            if (travelerActionGroup != null)
            {
                travelerActionGroup.SetActive(true);

                // 1. Bind the Check Distance Button
                if (checkLocationButton != null)
                {
                    checkLocationButton.onClick.RemoveAllListeners();
                    checkLocationButton.onClick.AddListener(OnCheckLocationClicked);
                }

                // 2. Bind the Open Map Button
                if (openMapButton != null)
                {
                    openMapButton.onClick.RemoveAllListeners();

                    if (data.TargetLatitude.HasValue && data.TargetLongitude.HasValue)
                    {
                        double lat = data.TargetLatitude.Value;
                        double lng = data.TargetLongitude.Value;

                        // Pass the coordinates directly to ChallengeActions
                        openMapButton.onClick.AddListener(() => _actions.OpenMapForTarget(lat, lng));
                        openMapButton.interactable = true;
                    }
                    else
                    {
                        // Lock the map button if a location hasn't been generated yet
                        openMapButton.interactable = false;
                    }
                }

                // 3. Configure Text and Button States based on generation
                if (!string.IsNullOrEmpty(data.TargetName) && data.TargetLatitude.HasValue && data.TargetLongitude.HasValue)
                {
                    targetDestinationText.text = $"Target: {data.TargetName}";
                    if (targetDestinationDistance != null) targetDestinationDistance.text = "";

                    if (checkLocationButton != null)
                    {
                        checkLocationButton.interactable = true;
                        checkLocationButton.GetComponentInChildren<TextMeshProUGUI>().text = "Check Distance";
                    }
                }
                else
                {
                    targetDestinationText.text = "GPS warming up. Tap button to search.";
                    if (targetDestinationDistance != null) targetDestinationDistance.text = "";

                    if (checkLocationButton != null)
                    {
                        checkLocationButton.interactable = true;
                        checkLocationButton.GetComponentInChildren<TextMeshProUGUI>().text = "Generate Location";
                    }

                    _actions.AutoGenerateGroupTravelerLocation(data, this);
                }
            }
        }

        // Configure Row Colors and Claim Button based on Status
        string status = (data.Status ?? "active").ToLower();

        if (status == "claimed")
        {
            buttonBackgroundImage.color = claimedColor;
            claimButton.interactable = false;
        }
        else if (status == "completed")
        {
            buttonBackgroundImage.color = claimableColor;
            claimButton.interactable = true;

            // Clean up UI by hiding the traveler actions once the challenge is complete
            if (travelerActionGroup != null) travelerActionGroup.SetActive(false);
        }
        else
        {
            buttonBackgroundImage.color = incompleteColor;
            claimButton.interactable = false;
        }

        detailsArea.SetActive(false);
    }

    #region Group Step Syncing & Calculations

    private async Task SyncAndLoadGroupSteps(GroupChallenge data)
    {
        progressText.text = "Syncing steps...";

        // 1. Get user's current today steps from local hardware
        UserController userCtrl = new UserController();
        int currentPersonalSteps = await userCtrl.GetTodayStepsAsync();

        // 2. Read how many steps this user already contributed to this specific challenge ID
        string prefsKey = "GroupStepsSynced_" + data.Id;
        int lastSyncedSteps = PlayerPrefs.GetInt(prefsKey, 0);

        // 3. Calculate how many NEW steps the user needs to contribute
        int delta = currentPersonalSteps - lastSyncedSteps;

        if (delta > 0)
        {
            Debug.Log($"[GroupChallenge] Contributing {delta} new steps to Group Challenge {data.Id}.");
            GroupChallengeController groupCtrl = new GroupChallengeController();

            // Push delta to Supabase (adds to 'step_progress')
            await groupCtrl.SyncStepsToGroupAsync(data.GroupId, delta);

            // Save the new baseline locally
            PlayerPrefs.SetInt(prefsKey, currentPersonalSteps);
            PlayerPrefs.Save();
        }

        // 4. Fetch the updated row from Supabase to display the correct combined group total
        var response = await SupabaseManager.Instance.From<GroupChallenge>()
            .Where(x => x.Id == data.Id)
            .Get();

        if (response.Models.Count > 0)
        {
            _data = response.Models[0];
            progressText.text = $"{_data.StepProgress} / {_data.StepTarget.Value} steps";

            // If group goal is met, refresh the panel to show the orange claim button
            if (_data.StepProgress >= _data.StepTarget.Value && _data.Status == "Active")
            {
                _uiManager.RefreshUI();
            }
        }
    }

    #endregion

    public void ToggleExpand()
    {
        string status = (_data.Status ?? "active").ToLower();
        if (status == "completed" || status == "claimed") return;

        if (!detailsArea.activeSelf)
        {
            _uiManager.CollapseAllOtherRows(this);
            detailsArea.SetActive(true);

            // --- THE EXPAND TRIGGER ---
            // Whenever the details panel is opened, force a fresh step sync in real-time [1.1]
            bool isStepChallenge = _data.StepTarget.HasValue && _data.StepTarget.Value > 0;
            if (isStepChallenge)
            {
                _ = SyncAndLoadGroupSteps(_data);
            }
        }
        else
        {
            detailsArea.SetActive(false);
        }
    }

    public void OnCheckLocationClicked()
    {
        if (_actions != null && _data != null)
        {
            _actions.ExecuteGroupTravelerChallenge(_data, this);
        }
    }

    public async void OnClaimRewardPressed()
    {
        claimButton.interactable = false;
        UserController userCtrl = new UserController();
        await userCtrl.UpdateUserAsync(_rewardCoins, _rewardXp, false);
        _data.Status = "claimed";
        await SupabaseManager.Instance.From<GroupChallenge>().Update(_data);
        _uiManager.RefreshUI();
    }
}