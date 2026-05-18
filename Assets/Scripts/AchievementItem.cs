using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using SupabaseModels;

public class AchievementItem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI achievText;
    [SerializeField] private Button claimButton;
    [SerializeField] private Image backgroundImage;

    [Header("Reward Texts")]
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private TextMeshProUGUI coinsText;

    [SerializeField] private CanvasGroup xpCanvasGroup;
    [SerializeField] private CanvasGroup coinsCanvasGroup;

    [Header("Animation")]
    [SerializeField] private float floatDistance = 80f;
    [SerializeField] private float animationDuration = 1.5f;

    [Header("Visuals")]
    [SerializeField]
    private Color claimedBackgroundColor =
        new Color(0.75f, 0.75f, 0.75f, 1f);

    [SerializeField]
    private Color claimedTextColor = Color.white;

    private Image claimButtonImage;

    private AchievementDefinition definition;
    private UserAchievement userAchievement;

    private UserAchievementController controller;

    private Vector2 xpStartPos;
    private Vector2 coinsStartPos;

    private void Awake()
    {
        if (achievText == null)
            achievText = GetComponentInChildren<TextMeshProUGUI>();

        if (claimButton == null)
            claimButton = GetComponentInChildren<Button>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        if (claimButton != null)
            claimButtonImage = claimButton.GetComponent<Image>();

        claimButton.onClick.AddListener(OnClaim);

        // Save starting positions
        xpStartPos = xpText.rectTransform.anchoredPosition;
        coinsStartPos = coinsText.rectTransform.anchoredPosition;

        HideRewardTexts();
    }

    public void Initialize(
        AchievementDefinition def,
        UserAchievement userAch)
    {
        definition = def;
        userAchievement = userAch;

        controller = new UserAchievementController();

        achievText.SetText(def.Title);

        xpText.SetText($"+{definition.XpReward} XP");
        coinsText.SetText($"+{definition.BalanceReward} C");

        RefreshUI();
    }

    public void Refresh(UserAchievement updatedAchievement)
    {
        userAchievement = updatedAchievement;

        RefreshUI();
    }

    private void RefreshUI()
    {
        bool unlocked = userAchievement.IsUnlocked;
        bool claimed = userAchievement.IsClaimed;

        // Locked state
        if (!unlocked)
        {
            claimButton.gameObject.SetActive(false);
        }
        else
        {
            claimButton.gameObject.SetActive(true);
        }

        // Button usability
        claimButton.interactable = unlocked && !claimed;

        // Claimed visuals
        if (claimed)
        {
            ApplyClaimedVisuals();
        }
    }

    private async void OnClaim()
    {
        if (!userAchievement.IsUnlocked || userAchievement.IsClaimed)
            return;

        Debug.Log($"[AchievementItem] Attempting to claim achievement ID: {userAchievement.Id}");

        // Give rewards
        CoinManager.Instance?.AddCoins(definition.BalanceReward);

        // Play animation
        StartCoroutine(AnimateRewardText(
            xpText,
            xpCanvasGroup,
            xpStartPos));

        StartCoroutine(AnimateRewardText(
            coinsText,
            coinsCanvasGroup,
            coinsStartPos));

        // Update DB
        await controller.ClaimAchievementAsync(userAchievement);

        // Verify the claim worked
        var verifiedAchievement = await controller.GetUserAchievementByIdAsync(userAchievement.Id);
        if (verifiedAchievement != null && verifiedAchievement.IsClaimed)
        {
            Debug.Log($"[AchievementItem] ✓ Successfully claimed achievement ID: {userAchievement.Id}");
            userAchievement.IsClaimed = true;
            RefreshUI();
        }
        else
        {
            Debug.LogError($"[AchievementItem] ✗ Failed to claim achievement ID: {userAchievement.Id}");
        }
    }

    private IEnumerator AnimateRewardText(
        TextMeshProUGUI text,
        CanvasGroup canvasGroup,
        Vector2 startPos)
    {
        float timer = 0f;

        RectTransform rect = text.rectTransform;

        rect.anchoredPosition = startPos;

        canvasGroup.alpha = 1f;

        text.gameObject.SetActive(true);

        Vector2 targetPos =
            startPos + Vector2.up * floatDistance;

        while (timer < animationDuration)
        {
            timer += Time.deltaTime;

            float t = timer / animationDuration;

            // Move upward
            rect.anchoredPosition =
                Vector2.Lerp(startPos, targetPos, t);

            // Fade out
            canvasGroup.alpha =
                Mathf.Lerp(1f, 0f, t);

            yield return null;
        }

        rect.anchoredPosition = startPos;

        canvasGroup.alpha = 0f;

        text.gameObject.SetActive(false);
    }

    private void HideRewardTexts()
    {
        xpCanvasGroup.alpha = 0f;
        coinsCanvasGroup.alpha = 0f;

        xpText.gameObject.SetActive(false);
        coinsText.gameObject.SetActive(false);
    }

    private void ApplyClaimedVisuals()
    {
        if (backgroundImage != null)
            backgroundImage.color = claimedBackgroundColor;

        if (achievText != null)
            achievText.color = claimedTextColor;

        if (claimButtonImage != null)
            claimButtonImage.color = claimedBackgroundColor;

        if (claimButton != null)
            claimButton.interactable = false;
    }
}