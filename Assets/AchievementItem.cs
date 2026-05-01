using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementItem : MonoBehaviour
{
    [Header("Rewards")]
    public int coinReward = 100;
    public int xpReward = 50;

    [Header("Internal References (auto-assigned if left empty)")]
    [SerializeField] private TextMeshProUGUI achievText;
    [SerializeField] private Button claimButton;
    [SerializeField] private Image backgroundImage;

    [Header("Claimed Visual Settings")]
    [SerializeField] private Color claimedBackgroundColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color claimedTextColor = Color.white;

    [Header("Claim Button Sprites")]
    [SerializeField] private Sprite claimedButtonSprite;

    private Image claimButtonImage;
    private bool isClaimed = false;

    private void Awake()
    {
        // Auto-find children if not assigned in Inspector
        if (achievText == null) achievText = GetComponentInChildren<TextMeshProUGUI>();
        if (claimButton == null) claimButton = GetComponentInChildren<Button>();
        if (backgroundImage == null) backgroundImage = GetComponent<Image>();

        // Cache button image
        if (claimButton != null)
            claimButtonImage = claimButton.GetComponent<Image>();

        claimButton.onClick.AddListener(OnClaim);
    }

    /// <summary>
    /// Call this to set the achievement's display text.
    /// </summary>
    public void SetTitle(string title)
    {
        if (achievText != null)
            achievText.SetText(title);
    }

    private void OnClaim()
    {
        if (isClaimed) return;
        isClaimed = true;

        // --- Give rewards ---
        if (CoinManager.Instance != null)
            CoinManager.Instance.AddCoins(coinReward);

        // XP:
        // XPManager.Instance?.AddXP(xpReward);

        // --- Gray out background ---
        if (backgroundImage != null)
            backgroundImage.color = claimedBackgroundColor;

        // --- Brighten text ---
        if (achievText != null)
            achievText.color = claimedTextColor;

        // --- Change button sprite ---
        if (claimButtonImage != null && claimedButtonSprite != null)
            claimButtonImage.sprite = claimedButtonSprite;

        // Disable button interaction
        if (claimButton != null)
            claimButton.interactable = false;
    }

    public bool IsClaimed => isClaimed;
}