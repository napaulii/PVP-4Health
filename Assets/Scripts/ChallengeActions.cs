using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using SupabaseModels;
using System.Threading.Tasks;

public class ChallengeActions : MonoBehaviour
{
    private string edgeFunctionUrl = "https://tqrbelbyudxdoovbybar.supabase.co/functions/v1/validate-meal";
    public ChallengeUIManager uiManager;
    private UserController _userController = new UserController();

    public void ExecuteChallengeAction(UserChallenge uc)
    {
        if (uc.ChallengeData.Type == "Healthy meal")
        {
            TakePhotoAndValidate(uc);
        }
        else
        {
            Debug.Log("Standard Challenge logic goes here.");
        }
    }

    private void TakePhotoAndValidate(UserChallenge uc)
    {
        NativeCamera.TakePicture((path) =>
        {
            if (path != null)
            {
                StartCoroutine(ProcessAndUpload(path, uc));
            }
        }, 1024);
    }

    private IEnumerator ProcessAndUpload(string path, UserChallenge uc)
    {
        byte[] imageBytes = System.IO.File.ReadAllBytes(path);
        string base64Image = System.Convert.ToBase64String(imageBytes);

        var requestData = new UploadRequest
        {
            imageBase64 = base64Image,
            userId = uc.UserId,
            challengeId = (int)uc.ChallengeId
        };

        string json = JsonUtility.ToJson(requestData);

        using (UnityWebRequest www = new UnityWebRequest(edgeFunctionUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            // --- SAFETY CHECK ADDED HERE ---
            string token = SupabaseManager.Instance.Auth.CurrentSession?.AccessToken;
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("[Upload Error] Auth token is missing! The user might be logged out or the session expired.");
                yield break;
            }
            www.SetRequestHeader("Authorization", "Bearer " + token);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AIResponse response = JsonUtility.FromJson<AIResponse>(www.downloadHandler.text);

                if (response.isHealthy)
                {
                    Debug.Log("AI Approved! Meal is healthy.");
                    int xp = uc.ChallengeData.XpReward;
                    int coins = uc.ChallengeData.BalanceReward;

                    // 2. Call your existing UserController method
                    // We use _ = to fire and forget or you can await it
                    _ = _userController.UpdateUserAsync(coins, xp, false);
                    uiManager.RefreshUI();
                }
                else
                {
                    Debug.LogWarning("AI Rejected: " + response.reason);
                }
            }
            else
            {
                // --- THE MAGIC LOGGING LINES ---
                Debug.LogError("Edge Function HTTP Status: " + www.responseCode);

                // This line will print the 79-byte secret message telling us exactly what is wrong!
                if (www.downloadHandler != null && !string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    Debug.LogError("EXACT SUPABASE ERROR: " + www.downloadHandler.text);
                }
                else
                {
                    Debug.LogError("Generic Unity Error: " + www.error);
                }
            }
        }
    }

    [System.Serializable]
    public class UploadRequest
    {
        public string imageBase64;
        public string userId;
        public int challengeId;
    }

    [System.Serializable]
    public class AIResponse
    {
        public bool isHealthy;
        public string reason;
    }
}