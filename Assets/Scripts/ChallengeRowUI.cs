using UnityEngine;
using TMPro;
using UnityEngine.UI;
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

        // FORCE CHECKMARK SETTINGS
        statusIcon.color = Color.white;
        statusIcon.material = null;

        string status = (data.Status ?? "active").ToLower();

        // --- APPLY SOLID COLORS (Using 255 Alpha) ---
        if (status == "claimed")
        {
            buttonBackgroundImage.color = claimedColor;
            claimButton.interactable = false;
        }
        else if (status == "completed")
        {
            buttonBackgroundImage.color = claimedColor;
            claimButton.interactable = true;
        }
        else
        {
            buttonBackgroundImage.color = incompleteColor;
            claimButton.interactable = false;

            string challengeType = (data.ChallengeData.Type ?? "").ToLower();

            bool isMeal = challengeType.Contains("meal");
            bool isTraveler = challengeType.Contains("traveler");

            // Toggle visibility of specialized UI panels
            photoActionGroup.SetActive(isMeal);
            travelerActionGroup.SetActive(isTraveler);
            stepActionGroup.SetActive(!isMeal && !isTraveler);

            if (isTraveler)
            {
                // Check if the destination is already assigned
                if (!string.IsNullOrEmpty(data.TargetName) && data.TargetLatitude.HasValue && data.TargetLongitude.HasValue)
                {
                    targetDestinationText.text = $"Target: {data.TargetName}";
                    checkLocationButton.interactable = true;
                    checkLocationButton.GetComponentInChildren<TextMeshProUGUI>().text = "Check Distance";
                }
                else
                {
                    // Automatically trigger location generation if fields are null/empty
                    targetDestinationText.text = "Locating nearby destination...";
                    checkLocationButton.interactable = false;
                    checkLocationButton.GetComponentInChildren<TextMeshProUGUI>().text = "Locating...";

                    _actions.AutoGenerateTravelerLocation(data);
                }
            }
        }

        detailsArea.SetActive(false);
    }

    public void ToggleExpand()
    {
        // LOCK EXPANSION IF FINISHED
        string status = _data.Status.ToLower();
        if (status == "completed" || status == "claimed")
        {
            return;
        }

        if (!detailsArea.activeSelf)
        {
            _uiManager.CollapseAllOtherRows(this);
            detailsArea.SetActive(true);
        }
        else
        {
            detailsArea.SetActive(false);
        }
    }

    public async void OnClaimRewardPressed()
    {
        // Disable immediately to prevent double-clicks
        claimButton.interactable = false;

        // 1. Grant Rewards
        UserController userCtrl = new UserController();
        await userCtrl.UpdateUserAsync(_data.ChallengeData.BalanceReward, _data.ChallengeData.XpReward, false);

        // 2. Mark as claimed in DB
        UserChallengeController ucCtrl = new UserChallengeController();
        await ucCtrl.UpdateUserChallengeStatusAsync(_data.Id, "claimed");

        // Update local data so the UI knows it changed
        _data.Status = "claimed";

        // 3. Refresh
        _uiManager.RefreshUI();
    }

    // Call this from the PChallengeRow's main Button component
    public void CloseDetails()
    {
        detailsArea.SetActive(false);
    }

    // Call this from the 'UploadPhotoButton' OnClick
    public void OnTakePhotoClicked()
    {
        _actions.ExecuteChallengeAction(_data);
    }

    // Call this from the 'CheckLocationButton' OnClick
    public void OnCheckLocationClicked()
    {
        _actions.ExecuteChallengeAction(_data);
    }
}