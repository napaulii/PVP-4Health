using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using SupabaseModels;

public class ChallengeRowUI : MonoBehaviour
{
    [Header("Colors")]
    public Color claimableColor;
    public Color incompleteColor;
    public Color claimedColor;

    [Header("Header References")]
    public TextMeshProUGUI descriptionText;
    public Button claimButton; // The checkmark button in the header
    public Image statusIcon;
    public Image buttonBackgroundImage; // The Image component ON the Button object

    [Header("Expandable Area")]
    public GameObject detailsArea; // Drag the 'DetailsArea' object here
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI coinsText;

    [Header("Action Area")]
    public GameObject photoActionGroup; // Drag 'UploadPhotoButton' here
    public GameObject stepActionGroup;  // Drag 'ProgressText' here

    [Header("Traveler Action Area")]
    public GameObject travelerActionGroup; // Drag 'TravelerActionGroup' here
    public TextMeshProUGUI targetDestinationText; // Drag 'TargetDestinationText' here
    public Button checkLocationButton; // Drag 'CheckLocationButton' here

    [Header("Sprites")]
    public Sprite cameraSprite;

    private UserChallenge _data;
    private ChallengeActions _actions;
    private ChallengeUIManager _uiManager;

    public void Setup(UserChallenge data, ChallengeActions actions, ChallengeUIManager uiManager)
    {
        _data = data;
        _actions = actions;
        _uiManager = uiManager;

        descriptionText.text = data.ChallengeData.Description;
        xpText.text = $"+{data.ChallengeData.XpReward} XP";
        coinsText.text = $"+{data.ChallengeData.BalanceReward} Coins";

        statusIcon.color = Color.white;
        statusIcon.material = null;

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
        }
        else
        {
            buttonBackgroundImage.color = incompleteColor;
            claimButton.interactable = false;

            string challengeType = (data.ChallengeData.Type ?? "").ToLower();

            bool isMeal = challengeType.Contains("meal");
            bool isTraveler = challengeType.Contains("traveler");

            photoActionGroup.SetActive(isMeal);
            travelerActionGroup.SetActive(isTraveler);
            stepActionGroup.SetActive(!isMeal && !isTraveler);

            if (isTraveler)
            {
                // DYNAMIC RUNTIME BINDING
                if (checkLocationButton != null)
                {
                    checkLocationButton.onClick.RemoveAllListeners();
                    checkLocationButton.onClick.AddListener(OnCheckLocationClicked);
                }
                else
                {
                    Debug.LogError("[ChallengeRowUI Error] 'checkLocationButton' is null on " + gameObject.name);
                }

                if (!string.IsNullOrEmpty(data.TargetName) && data.TargetLatitude.HasValue && data.TargetLongitude.HasValue)
                {
                    targetDestinationText.text = $"Target: {data.TargetName}";
                    checkLocationButton.interactable = true;
                    checkLocationButton.GetComponentInChildren<TextMeshProUGUI>().text = "Check Distance";
                }
                else
                {
                    targetDestinationText.text = "Locating nearby destination...";
                    checkLocationButton.interactable = false;
                    checkLocationButton.GetComponentInChildren<TextMeshProUGUI>().text = "Locating...";

                    _actions.AutoGenerateTravelerLocation(data, this);
                }
            }
            else if (!isMeal && !isTraveler)
            {
                // This is a Step Challenge. Update the progress text.
                UpdateStepProgress(data);
            }
        }

        detailsArea.SetActive(false);
    }

    #region Step Count Logic

    private async void UpdateStepProgress(UserChallenge data)
    {
        var textComp = stepActionGroup.GetComponent<TextMeshProUGUI>();
        if (textComp == null) return;

        textComp.text = "Reading steps...";

        UserController userCtrl = new UserController();
        int steps = await userCtrl.GetTodayStepsAsync(); // Reads locally and calculates against database

        int target = ExtractTargetFromDescription(data.ChallengeData.Description);

        // Re-enable the standard user-friendly step counter format
        textComp.text = $"{steps} / {target} steps";

        if (steps >= target && data.Status.ToLower() == "active")
        {
            await MarkStepChallengeAsCompleted(data);
        }
    }

    /// <summary>
    /// Extracts all digits from the description string to determine target step count.
    /// E.g., "Walk 5000 steps" -> 5000
    /// </summary>
    private int ExtractTargetFromDescription(string desc)
    {
        string numStr = "";
        foreach (char c in desc)
        {
            if (char.IsDigit(c)) numStr += c;
        }
        if (int.TryParse(numStr, out int target)) return target;
        return 5000; // Fallback default
    }

    private async Task MarkStepChallengeAsCompleted(UserChallenge data)
    {
        data.Status = "completed";
        UserChallengeController ucCtrl = new UserChallengeController();
        await ucCtrl.UpdateUserChallengeStatusAsync(data.Id, "completed");
        _uiManager.RefreshUI();
    }

    #endregion

    public void ToggleExpand()
    {
        string status = _data.Status.ToLower();
        if (status == "completed" || status == "claimed")
        {
            return;
        }

        if (!detailsArea.activeSelf)
        {
            _uiManager.CollapseAllOtherRows(this);
            detailsArea.SetActive(true);

            // --- THE UPDATE TRIGGER ---
            // If the row is expanding, check if it's a Step challenge and refresh the counter
            string challengeType = (_data.ChallengeData.Type ?? "").ToLower();
            if (!challengeType.Contains("meal") && !challengeType.Contains("traveler"))
            {
                UpdateStepProgress(_data);
            }
        }
        else
        {
            detailsArea.SetActive(false);
        }
    }

    public async void OnClaimRewardPressed()
    {
        claimButton.interactable = false;

        UserController userCtrl = new UserController();
        await userCtrl.UpdateUserAsync(_data.ChallengeData.BalanceReward, _data.ChallengeData.XpReward, false);

        UserChallengeController ucCtrl = new UserChallengeController();
        await ucCtrl.UpdateUserChallengeStatusAsync(_data.Id, "claimed");

        _data.Status = "claimed";
        _uiManager.RefreshUI();
    }

    public void CloseDetails()
    {
        detailsArea.SetActive(false);
    }

    public void OnTakePhotoClicked()
    {
        _actions.ExecuteChallengeAction(_data, this);
    }

    public void OnCheckLocationClicked()
    {
        // 1. Verify if Text reference is missing
        if (targetDestinationText == null)
        {
            Debug.LogError("[ChallengeRowUI Error] 'targetDestinationText' reference is missing/unassigned in the Inspector!");
        }

        // 2. Verify if ChallengeActions is missing
        if (_actions == null)
        {
            Debug.LogError("[ChallengeRowUI Error] '_actions' (ChallengeActions) reference is null on this row! " +
                           "Please ensure that the 'Action Manager' slot on your ChallengeUIManager GameObject is assigned in the Inspector.");

            if (targetDestinationText != null)
            {
                targetDestinationText.text = "Error: Action Manager Null";
            }
            return;
        }

        // 3. Verify if database row data is missing
        if (_data == null)
        {
            Debug.LogError("[ChallengeRowUI Error] '_data' (UserChallenge row data) is null on this row!");
            return;
        }

        // Run the action
        _actions.ExecuteChallengeAction(_data, this);
    }

}