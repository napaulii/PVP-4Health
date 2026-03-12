using System;
using System.Threading.Tasks;
using UnityEngine;
using FortressBuilder.Network; // Gives access to BackendAPI
using FortressBuilder.Core;

namespace FortressBuilder.Economy
{
    [Serializable]
    public class UserData
    {
        public int id;
        public string nickname;
        public int balance;
        public int xp;
    }

    [Serializable]
    public class ChallengeData
    {
        public int id;
        public string type;
        public string description;
        public int reward;
    }

    [Serializable]
    public class AchievementData
    {
        public int id;
        public string title;
        public string description;
    }

    // ==========================================
    // CURRENCY MANAGER
    // ==========================================
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        [Header("Local Player Data")]
        [SerializeField] private UserData localUser;

        // Event triggered whenever the balance changes. UI scripts should subscribe to this.
        public event Action<int> OnBalanceUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Initializes the manager with data fetched at login.
        /// </summary>
        public void Initialize(UserData userData)
        {
            localUser = userData;
            OnBalanceUpdated?.Invoke(localUser.balance);
        }

        // ==========================================
        // SERVER SYNC LOGIC
        // ==========================================

        /// <summary>
        /// Updates the local client balance strictly based on what the server says is true.
        /// </summary>
        private void SyncBalanceWithServer(int newServerBalance, string reason)
        {
            if (localUser.balance != newServerBalance)
            {
                localUser.balance = newServerBalance;
                Debug.Log($"<color=cyan>Server Sync</color>: Balance is now {localUser.balance}. Reason: {reason}");

                // Notify UI to update
                OnBalanceUpdated?.Invoke(localUser.balance);
            }
        }

        // ==========================================
        // ACCUMULATION METHODS
        // ==========================================

        public async void CompleteUserChallenge(ChallengeData completedChallenge)
        {
            Debug.Log($"Notifying server: Completed solo challenge {completedChallenge.id}...");

            ServerResponse response = await BackendAPI.Instance.CompleteUserChallenge(localUser.id, completedChallenge.id);

            if (response != null && response.success)
            {
                localUser.xp = response.newXp; // Update XP if returned
                SyncBalanceWithServer(response.newBalance, $"Completed Solo Challenge: {completedChallenge.id}");
            }
            else
            {
                string errorMsg = response != null ? response.message : "Network Error";
                Debug.LogError($"Failed to complete challenge on server: {errorMsg}");
            }
        }

        public async void CompleteGroupChallenge(int groupId, ChallengeData completedChallenge)
        {
            Debug.Log($"Notifying server: Group {groupId} completed challenge {completedChallenge.id}...");

            ServerResponse response = await BackendAPI.Instance.CompleteGroupChallenge(localUser.id, groupId, completedChallenge.id);

            if (response != null && response.success)
            {
                // Sync personal money
                SyncBalanceWithServer(response.newBalance, $"Completed Group Challenge: {completedChallenge.id}");

                // --> NEW: Sync the Fortress with the server's absolute truth!
                Fortress myFortress = FindAnyObjectByType<Fortress>();
                if (myFortress != null)
                {
                    // We use SyncWithServer instead of AddXP because the Server 
                    // has already done the math and is telling us the exact new level/xp.
                    myFortress.SyncWithServer(response.newFortressLevel, response.newFortressXp);
                }
            }
        }

        public async void UnlockAchievement(AchievementData achievement)
        {
            Debug.Log($"Notifying server: Unlocked achievement {achievement.id}...");

            ServerResponse response = await BackendAPI.Instance.UnlockAchievement(localUser.id, achievement.id);

            if (response != null && response.success)
            {
                SyncBalanceWithServer(response.newBalance, $"Unlocked Achievement: {achievement.title}");
            }
            else
            {
                string errorMsg = response != null ? response.message : "Network Error";
                Debug.LogError($"Failed to unlock achievement on server: {errorMsg}");
            }
        }

        // ==========================================
        // SPENDING METHOD (Shop)
        // ==========================================

        /// <summary>
        /// Attempts to buy an item by validating with the server. Returns true if successful.
        /// </summary>
        public async Task<bool> TryBuyItem(int itemId, int localPriceEstimate)
        {
            // Client-side quick check (optional, prevents unnecessary network calls)
            if (localUser.balance < localPriceEstimate)
            {
                Debug.LogWarning("Not enough local balance. Aborting request.");
                return false;
            }

            Debug.Log($"Requesting purchase for item {itemId}...");

            ServerResponse response = await BackendAPI.Instance.BuyPersonalItem(localUser.id, itemId);

            if (response != null && response.success)
            {
                SyncBalanceWithServer(response.newBalance, $"Purchased item: {itemId}");
                return true; // The UI can use this bool to show a "Purchase Successful" popup
            }

            string errorMsg = response != null ? response.message : "Network Error";
            Debug.LogError($"Purchase failed: {errorMsg}");
            return false;
        }

        // Getter for UI scripts that just need to poll the current amount
        public int GetCurrentBalance() => localUser != null ? localUser.balance : 0;
    }
}