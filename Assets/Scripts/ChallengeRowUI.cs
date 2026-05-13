using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SupabaseModels;

public class ChallengeRowUI : MonoBehaviour
{
    [Header("Header References")]
    public TextMeshProUGUI descriptionText;
    public Button claimButton; // The checkmark button in the header
    public Image statusIcon;

    [Header("Expandable Area")]
    public GameObject detailsArea; // Drag the 'DetailsArea' object here
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI coinsText;

    [Header("Action Area")]
    public GameObject photoActionGroup; // Drag 'UploadPhotoButton' here
    public GameObject stepActionGroup;  // Drag 'ProgressText' here

    [Header("Sprites")]
    public Sprite checkmarkSprite;
    public Sprite cameraSprite;

    private UserChallenge _data;
    private ChallengeActions _actions;

    public void Setup(UserChallenge data, ChallengeActions actions)
    {
        _data = data;
        _actions = actions;

        // 1. Setup Header
        descriptionText.text = data.ChallengeData.Description;

        // 2. Setup Rewards (inside details)
        xpText.text = $"+{data.ChallengeData.XpReward} XP";
        coinsText.text = $"+{data.ChallengeData.BalanceReward} Coins";

        // 3. Set visibility of specific actions
        bool isMeal = data.ChallengeData.Type.ToLower().Contains("meal");
        photoActionGroup.SetActive(isMeal);
        stepActionGroup.SetActive(!isMeal);

        // 4. Start Collapsed
        detailsArea.SetActive(false);

        // 5. Icon Logic
        if (string.Equals(data.Status, "completed", System.StringComparison.OrdinalIgnoreCase))
        {
            statusIcon.sprite = checkmarkSprite;
            statusIcon.color = Color.green;
            claimButton.interactable = true; // Click to claim!
        }
        else
        {
            statusIcon.sprite = isMeal ? cameraSprite : checkmarkSprite;
            statusIcon.color = Color.white;
            claimButton.interactable = false; // Locked until AI/Steps finish
        }
    }

    // Call this from the PChallengeRow's main Button component
    public void ToggleExpand()
    {
        detailsArea.SetActive(!detailsArea.activeSelf);
        // Because of Content Size Fitter, the box will grow/shrink automatically
    }

    // Call this from the 'UploadPhotoButton' OnClick
    public void OnTakePhotoClicked()
    {
        _actions.ExecuteChallengeAction(_data);
    }
}