using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SupabaseModels;

public class ChallengeRowUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI descriptionText;
    public Image statusIcon; // Drag the Image object from under the Button here
    public Button actionButton;

    [Header("Sprites")]
    public Sprite checkmarkSprite; // Assign in Inspector
    public Sprite cameraSprite;    // Assign in Inspector

    private UserChallenge _data;
    private ChallengeActions _actions;

    public void Setup(UserChallenge data, ChallengeActions actions)
    {
        _data = data;
        _actions = actions;

        // --- DEBUG CHECKS ---
        if (descriptionText == null)
        {
            Debug.LogError($"Row {gameObject.name} is missing its Description Text reference in the Inspector!");
            return;
        }
        if (data.ChallengeData == null)
        {
            Debug.LogError($"Row {gameObject.name} received null ChallengeData! The database join failed.");
            return;
        }
        // ---------------------

        descriptionText.text = data.ChallengeData.Description;

        if (data.ChallengeData.Type == "Healthy meal")
        {
            statusIcon.sprite = cameraSprite;
        }
        else
        {
            statusIcon.sprite = checkmarkSprite;
        }

        if (data.Status == "completed")
        {
            statusIcon.color = Color.green;
            if (actionButton != null) actionButton.interactable = false;
        }
        else
        {
            statusIcon.color = Color.white;
            if (actionButton != null) actionButton.interactable = true;
        }
    }

    // Call this from the Button's OnClick in Inspector
    public void OnIconButtonClicked()
    {
        _actions.ExecuteChallengeAction(_data);
    }
}