using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("Starting coins")]
    [SerializeField] private int startingCoins = 1000;

    [Header("Coin display labels")]
    [SerializeField] private TextMeshProUGUI[] coinLabels;

    public UnityEvent<int> OnCoinsChanged = new UnityEvent<int>();

    public int Coins { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Coins = startingCoins;
        UpdateAllLabels();
    }

    public bool TrySpend(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        UpdateAllLabels();
        OnCoinsChanged.Invoke(Coins);
        return true;
    }

    public void AddCoins(int amount)
    {
        Coins += amount;
        UpdateAllLabels();
        OnCoinsChanged.Invoke(Coins);
    }

    private void UpdateAllLabels()
    {
        if (coinLabels == null) return;
        foreach (TextMeshProUGUI label in coinLabels)
            if (label != null)
                label.SetText(Coins.ToString());
    }
}