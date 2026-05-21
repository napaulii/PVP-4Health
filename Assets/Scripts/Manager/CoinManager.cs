using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;           // <-- add this
using SupabaseModels;
using User = SupabaseModels.User;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("Security")]
    [SerializeField] private string loginSceneName = "Login";

    [Header("Coin Sprite")]
    [SerializeField] private Sprite coinSprite;         // drag your sprite here

    [Header("Coin Displays")]
    [SerializeField] private CoinDisplay[] coinDisplays; // pairs label + icon

    [Serializable]
    public struct CoinDisplay
    {
        public TextMeshProUGUI label;
        public Image icon;          // place an Image component AFTER your label in the UI
    }

    public UnityEvent<int> OnCoinsChanged = new UnityEvent<int>();
    public int Coins { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private async void Start()
    {
        Debug.Log("<color=cyan>[Home Scene]</color> Entered Home Scene. Initializing CoinManager...");
        await RefreshBalanceFromServer();
    }

    public async Task RefreshBalanceFromServer()
    {
        try
        {
            var currentUser = SupabaseManager.Instance.Auth.CurrentUser;

            if (currentUser == null)
            {
                Debug.LogError("<color=red>[Illegal Entry]</color> No active user session found! Redirecting to Login scene...");
                SceneManager.LoadScene(loginSceneName);
                return;
            }

            Debug.Log($"<color=green>[Login Verified]</color> User ID: {currentUser.Id}");

            var response = await SupabaseManager.Instance
                .From<User>()
                .Where(u => u.Id == currentUser.Id)
                .Single();

            if (response != null)
            {
                Coins = response.Balance;
                Debug.Log($"<color=yellow>[Data Synced]</color> Balance: {Coins} coins.");
                UpdateAllLabels();
                OnCoinsChanged.Invoke(Coins);
                if (AchievementChecker.Instance != null)
                    await AchievementChecker.Instance.CheckAchievementsAsync();
            }
            else
            {
                Debug.LogWarning("[CoinManager] Auth exists but profile missing from DB. Redirecting.");
                SceneManager.LoadScene(loginSceneName);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CoinManager] Critical Error: {ex.Message}");
            SceneManager.LoadScene(loginSceneName);
        }
    }

    public async Task<bool> TrySpend(int amount)
    {
        if (Coins < amount) return false;

        int newBalance = Coins - amount;
        bool success = await UpdateDatabaseBalance(newBalance);
        if (success)
        {
            Coins = newBalance;
            UpdateAllLabels();
            OnCoinsChanged.Invoke(Coins);
            return true;
        }
        return false;
    }

    public async Task AddCoins(int amount)
    {
        int newBalance = Coins + amount;
        bool success = await UpdateDatabaseBalance(newBalance);
        if (success)
        {
            Coins = newBalance;
            UpdateAllLabels();
            OnCoinsChanged.Invoke(Coins);
            if (AchievementChecker.Instance != null)
                await AchievementChecker.Instance.CheckAchievementsAsync();
        }
    }

    private async Task<bool> UpdateDatabaseBalance(int newBalance)
    {
        try
        {
            var userId = SupabaseManager.Instance.Auth.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId)) return false;

            await SupabaseManager.Instance
                .From<User>()
                .Where(u => u.Id == userId)
                .Set(u => u.Balance, newBalance)
                .Update();

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CoinManager] DB Update Failed: {ex.Message}");
            return false;
        }
    }

    private void UpdateAllLabels()
    {
        if (coinDisplays == null) return;
        foreach (CoinDisplay display in coinDisplays)
        {
            if (display.label != null)
                display.label.SetText(Coins.ToString("N0"));

            if (display.icon != null && coinSprite != null)
                display.icon.sprite = coinSprite;
        }
    }
}