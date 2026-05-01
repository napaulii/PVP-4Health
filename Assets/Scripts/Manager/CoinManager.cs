using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using SupabaseModels; 
using User = SupabaseModels.User; 

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("Security")]
    [SerializeField] private string loginSceneName = "Login"; // Set this to your actual login scene name

    [Header("Coin display labels")]
    [SerializeField] private TextMeshProUGUI[] coinLabels;

    public UnityEvent<int> OnCoinsChanged = new UnityEvent<int>();
    public int Coins { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private async void Start()
    {
        // 1. Initial Scene Entry Log
        Debug.Log("<color=cyan>[Home Scene]</color> Entered Home Scene. Initializing CoinManager...");
        
        await RefreshBalanceFromServer();
    }

    public async Task RefreshBalanceFromServer()
    {
        try
        {
            // 1. Grab the current user from Supabase Auth
            var currentUser = SupabaseManager.Instance.Auth.CurrentUser;
            
            // 2. SECURITY CHECK: If no user is found, redirect to Login
            if (currentUser == null)
            {
                Debug.LogError("<color=red>[Illegal Entry]</color> No active user session found! Redirecting to Login scene...");
                SceneManager.LoadScene(loginSceneName);
                return; // Stop execution here
            }

            Debug.Log($"<color=green>[Login Verified]</color> User ID: {currentUser.Id}");

            // 3. Fetch the user profile from the database
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
            }
            else
            {
                // This handles cases where Auth exists but the DB profile was never created
                Debug.LogWarning("[CoinManager] Auth exists but profile missing from DB. Redirecting to onboarding/login.");
                SceneManager.LoadScene(loginSceneName);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CoinManager] Critical Error: {ex.Message}");
            SceneManager.LoadScene(loginSceneName);
        }
    }

    /// <summary>
    /// Deducts coins from the database. Returns false if insufficient funds or DB error.
    /// </summary>
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

    /// <summary>
    /// Adds coins to the database.
    /// </summary>
    public async Task AddCoins(int amount)
    {
        int newBalance = Coins + amount;

        bool success = await UpdateDatabaseBalance(newBalance);
        if (success)
        {
            Coins = newBalance;
            UpdateAllLabels();
            OnCoinsChanged.Invoke(Coins);
        }
    }

    private async Task<bool> UpdateDatabaseBalance(int newBalance)
    {
        try
        {
            var userId = SupabaseManager.Instance.Auth.CurrentUser?.Id;
            if (string.IsNullOrEmpty(userId)) return false;

            // Update only the balance column for this specific user
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
        if (coinLabels == null) return;
        foreach (TextMeshProUGUI label in coinLabels)
        {
            if (label != null)
                label.SetText(Coins.ToString("N0")); // "N0" adds thousand separators (e.g., 1,000)
        }
    }
}