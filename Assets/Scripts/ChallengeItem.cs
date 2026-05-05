using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChallengeItem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI challengeText;
    [SerializeField] private Button completeButton;

    public int coinReward;
    private bool completed = false;

    public void Setup(string title, int coins)
    {
        coinReward = coins;
        if (challengeText != null) challengeText.SetText(title);
        if (completeButton != null)
            completeButton.onClick.AddListener(OnComplete);
    }

    private async void OnComplete()
    {
        if (completed) return;
        completed = true;

        completeButton.interactable = false;

        if (coinReward > 0 && CoinManager.Instance != null)
            await CoinManager.Instance.AddCoins(coinReward);

        // Optional: visually mark as done
        var img = completeButton.GetComponent<Image>();
        if (img != null) img.color = Color.gray;
    }
}