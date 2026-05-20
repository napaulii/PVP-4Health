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
            // Solid Green
            buttonBackgroundImage.color = claimedColor;
            claimButton.interactable = false;
        }
        else if (status == "completed")
        {
            // Solid Orange
            buttonBackgroundImage.color = claimedColor;
            claimButton.interactable = true;
        }
        else
        {
            // Solid Dull Red/Brown
            buttonBackgroundImage.color = incompleteColor;
            claimButton.interactable = false;

            bool isMeal = data.ChallengeData.Type.ToLower().Contains("meal");
            photoActionGroup.SetActive(isMeal);
            stepActionGroup.SetActive(!isMeal);
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
}