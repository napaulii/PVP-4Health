using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace FortressBuilder.Network
{
    // ==========================================
    // DATA TRANSFER OBJECTS (DTOs)
    // ==========================================
    [Serializable]
    public class ChallengeRequest
    {
        public int userId;
        public int challengeId;
    }

    [Serializable]
    public class GroupChallengeRequest
    {
        public int userId;
        public int groupId;
        public int challengeId;
    }

    [Serializable]
    public class AchievementRequest
    {
        public int userId;
        public int achievementId;
    }

    [Serializable]
    public class ShopRequest
    {
        public int userId;
        public int itemId;
    }

    [Serializable]
    public class ServerResponse
    {
        public bool success;
        public string message;
        public int newBalance;
        public int newXp;
        public int newFortressLevel;
        public int newFortressXp;
    }
    [Serializable]
    public class FortressResponse
    {
        public bool success;
        public string message;
        public int level;
        public int currentXp;
    }
    public class BackendAPI : MonoBehaviour
    {
        public static BackendAPI Instance { get; private set; }

        [Header("API Settings")]
        [Tooltip("The base URL of your backend server (e.g., https://api.mygame.com/v1)")]
        [SerializeField] private string baseUrl = "http://localhost:3000/api";

        [Tooltip("Auth token if you have implemented user login/sessions")]
        [SerializeField] private string authToken = "";

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

        // ==========================================
        // SPECIFIC ENDPOINTS
        // ==========================================

        /// <summary>
        /// Tells the server the user completed a solo challenge.
        /// </summary>
        public async Task<ServerResponse> CompleteUserChallenge(int userId, int challengeId)
        {
            string url = $"{baseUrl}/challenges/user/complete";
            var requestData = new ChallengeRequest { userId = userId, challengeId = challengeId };
            return await PostJsonAsync<ServerResponse>(url, requestData);
        }

        /// <summary>
        /// Tells the server a group challenge was completed.
        /// </summary>
        public async Task<ServerResponse> CompleteGroupChallenge(int userId, int groupId, int challengeId)
        {
            string url = $"{baseUrl}/challenges/group/complete";
            var requestData = new GroupChallengeRequest { userId = userId, groupId = groupId, challengeId = challengeId };
            return await PostJsonAsync<ServerResponse>(url, requestData);
        }

        /// <summary>
        /// Tells the server an achievement was unlocked.
        /// </summary>
        public async Task<ServerResponse> UnlockAchievement(int userId, int achievementId)
        {
            string url = $"{baseUrl}/achievements/unlock";
            var requestData = new AchievementRequest { userId = userId, achievementId = achievementId };
            return await PostJsonAsync<ServerResponse>(url, requestData);
        }

        /// <summary>
        /// Tells the server the user wants to buy an item.
        /// </summary>
        public async Task<ServerResponse> BuyPersonalItem(int userId, int itemId)
        {
            string url = $"{baseUrl}/shop/buy";
            var requestData = new ShopRequest { userId = userId, itemId = itemId };
            return await PostJsonAsync<ServerResponse>(url, requestData);
        }
        public async Task<FortressResponse> GetFortressData(int groupId)
        {
            // Example URL: http://localhost:3000/api/fortress/5
            string url = $"{baseUrl}/fortress/{groupId}";

            return await GetJsonAsync<FortressResponse>(url);
        }
        // ==========================================
        // CORE NETWORK HELPERS
        // ==========================================

        /// <summary>
        /// A generic helper method to send POST requests with JSON payloads.
        /// </summary>
        private async Task<T> PostJsonAsync<T>(string url, object postData)
        {
            string jsonBody = JsonUtility.ToJson(postData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                request.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(authToken))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {authToken}");
                }

                // Send request and wait for completion
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"[API Error] {url}: {request.error}\nResponse: {request.downloadHandler.text}");
                    return default;
                }

                string responseJson = request.downloadHandler.text;
                Debug.Log($"[API Success] {url}: {responseJson}");

                return JsonUtility.FromJson<T>(responseJson);
            }
        }
        private async Task<T> GetJsonAsync<T>(string url)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                if (!string.IsNullOrEmpty(authToken))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {authToken}");
                }

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"[API Error] GET {url}: {request.error}\nResponse: {request.downloadHandler.text}");
                    return default;
                }

                string responseJson = request.downloadHandler.text;
                Debug.Log($"[API Success] GET {url}: {responseJson}");

                return JsonUtility.FromJson<T>(responseJson);
            }
        }
    }
}