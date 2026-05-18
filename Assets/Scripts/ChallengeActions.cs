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
        // 1. Load image from phone storage and convert to string for the AI
        byte[] imageBytes = System.IO.File.ReadAllBytes(path);
        // Optimization: If the app is slow, consider using Texture2D.EncodeToJPG(30) here instead
        string base64Image = System.Convert.ToBase64String(imageBytes);

        // 2. Prepare the data object to send to the Edge Function
        var requestData = new UploadRequest
        {
            imageBase64 = base64Image,
            userId = uc.UserId,
            challengeId = (int)uc.ChallengeId
        };

        string json = JsonUtility.ToJson(requestData);

        // 3. Create the Web Request
        using (UnityWebRequest www = new UnityWebRequest(edgeFunctionUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();

            www.SetRequestHeader("Content-Type", "application/json");

            // 4. Set Authorization Header
            string token = SupabaseManager.Instance.Auth.CurrentSession?.AccessToken;
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("[Upload Error] User session is invalid. Please log in again.");
                yield break;
            }
            www.SetRequestHeader("Authorization", "Bearer " + token);

            // 5. Send to Supabase Cloud
            Debug.Log("Sending photo to Gemini AI for validation...");
            yield return www.SendWebRequest();

            // 6. Handle the Response
            if (www.result == UnityWebRequest.Result.Success)
            {
                AIResponse response = JsonUtility.FromJson<AIResponse>(www.downloadHandler.text);

                if (response.isHealthy)
                {
                    Debug.Log("AI Approved! Challenge is now ready to claim.");

                    // We DON'T give XP/Coins here anymore.
                    // We just refresh the UI so the user can see the Orange button and click it themselves.
                    uiManager.RefreshUI();
                }
                else
                {
                    // AI looked at the photo but didn't see a healthy meal
                    Debug.LogWarning("AI Rejected: " + response.reason);
                    // Tip: You could show response.reason in a UI popup here
                }
            }
            else
            {
                // 7. Detailed Error Logging
                Debug.LogError($"Edge Function Error ({www.responseCode})");

                if (www.downloadHandler != null && !string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    // This prints the actual error message from your index.ts code
                    Debug.LogError("SERVER MESSAGE: " + www.downloadHandler.text);
                }
                else
                {
                    Debug.LogError("GENERIC ERROR: " + www.error);
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