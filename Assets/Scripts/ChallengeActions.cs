using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using SupabaseModels;

public class ChallengeActions : MonoBehaviour
{
    [Header("Meal Challenge Config")]
    [SerializeField] private string edgeFunctionUrl = "https://tqrbelbyudxdoovbybar.supabase.co/functions/v1/validate-meal";

    [Header("Traveler Challenge Config")]
    [SerializeField] private string googleApiKey = "AIzaSyCk4K5x72HLMEdTbs-rIhmZP4xFvSuQIfo";
    [SerializeField] private double arrivalThresholdMeters = 100.0;

    [Header("References")]
    public ChallengeUIManager uiManager;

    private UserController _userController = new UserController();
    private bool _isGeneratingLocation = false;

    public void ExecuteChallengeAction(UserChallenge uc)
    {
        string challengeType = (uc.ChallengeData?.Type ?? "").ToLower();

        if (challengeType.Contains("meal"))
        {
            TakePhotoAndValidate(uc);
        }
        else if (challengeType.Contains("traveler"))
        {
            StartCoroutine(HandleTravelerChallenge(uc));
        }
        else
        {
            Debug.Log("Standard Challenge logic goes here.");
        }
    }

    /// <summary>
    /// Public entry point called by ChallengeRowUI to trigger background generation
    /// </summary>
    public void AutoGenerateTravelerLocation(UserChallenge uc)
    {
        if (_isGeneratingLocation) return;
        StartCoroutine(HandleAutoGenerationFlow(uc));
    }

    #region Healthy Meal Action Logic

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

            string token = SupabaseManager.Instance.Auth.CurrentSession?.AccessToken;
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogError("[Upload Error] User session is invalid. Please log in again.");
                yield break;
            }
            www.SetRequestHeader("Authorization", "Bearer " + token);

            Debug.Log("Sending photo to Gemini AI for validation...");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AIResponse response = JsonUtility.FromJson<AIResponse>(www.downloadHandler.text);

                if (response.isHealthy)
                {
                    Debug.Log("AI Approved! Challenge is now ready to claim.");
                    uiManager.RefreshUI();
                }
                else
                {
                    Debug.LogWarning("AI Rejected: " + response.reason);
                }
            }
            else
            {
                Debug.LogError($"Edge Function Error ({www.responseCode})");

                if (www.downloadHandler != null && !string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    Debug.LogError("SERVER MESSAGE: " + www.downloadHandler.text);
                }
                else
                {
                    Debug.LogError("GENERIC ERROR: " + www.error);
                }
            }
        }
    }

    #endregion

    #region Traveler Action Logic

    private IEnumerator HandleAutoGenerationFlow(UserChallenge uc)
    {
        _isGeneratingLocation = true;

        if (GPSManager.Instance == null)
        {
            Debug.LogError("GPSManager instance is missing in the scene.");
            _isGeneratingLocation = false;
            yield break;
        }

        var locationTask = GPSManager.Instance.GetCurrentLocationAsync();
        yield return new WaitUntil(() => locationTask.IsCompleted);
        var coords = locationTask.Result;

        if (coords == null)
        {
            Debug.LogError("Could not retrieve GPS coordinates for auto-generation.");
            _isGeneratingLocation = false;
            yield break;
        }

        yield return StartCoroutine(FetchNearbyPlace(coords.Value.lat, coords.Value.lng, uc));
        _isGeneratingLocation = false;
    }

    private IEnumerator HandleTravelerChallenge(UserChallenge uc)
    {
        if (GPSManager.Instance == null)
        {
            Debug.LogError("GPSManager instance is missing in the scene.");
            yield break;
        }

        var locationTask = GPSManager.Instance.GetCurrentLocationAsync();
        yield return new WaitUntil(() => locationTask.IsCompleted);
        var coords = locationTask.Result;

        if (coords == null)
        {
            Debug.LogError("Could not retrieve GPS coordinates. Please ensure location services are enabled on your device.");
            yield break;
        }

        double userLat = coords.Value.lat;
        double userLng = coords.Value.lng;

        double targetLat = uc.TargetLatitude ?? 0;
        double targetLng = uc.TargetLongitude ?? 0;

        double distance = CalculateDistance(userLat, userLng, targetLat, targetLng);
        Debug.Log($"Distance to {uc.TargetName}: {distance:F1} meters.");

        if (distance <= arrivalThresholdMeters)
        {
            Debug.Log("Arrival threshold met. Updating challenge status...");
            yield return StartCoroutine(CompleteChallenge(uc));
        }
        else
        {
            Debug.Log($"You are still {distance:F1} meters away from your target destination.");
        }
    }

    private IEnumerator FetchNearbyPlace(double lat, double lng, UserChallenge uc)
    {
        string url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={lat},{lng}&radius=5000&type=tourist_attraction|museum|park&key={googleApiKey}";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                GooglePlacesResponse response = JsonUtility.FromJson<GooglePlacesResponse>(www.downloadHandler.text);

                if (response?.results != null && response.results.Length > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, response.results.Length);
                    var place = response.results[randomIndex];

                    uc.TargetName = place.name;
                    uc.TargetLatitude = place.geometry.location.lat;
                    uc.TargetLongitude = place.geometry.location.lng;

                    var updateTask = SupabaseManager.Instance.From<UserChallenge>().Update(uc);
                    yield return new WaitUntil(() => updateTask.IsCompleted);

                    Debug.Log($"Automatically assigned destination: {uc.TargetName}");
                    uiManager.RefreshUI();
                }
                else
                {
                    Debug.LogWarning("No suitable locations found within a 5km radius.");
                }
            }
            else
            {
                Debug.LogError($"Google Places API request failed: {www.error}");
            }
        }
    }

    private IEnumerator CompleteChallenge(UserChallenge uc)
    {
        uc.Status = "completed";
        var updateTask = SupabaseManager.Instance.From<UserChallenge>().Update(uc);
        yield return new WaitUntil(() => updateTask.IsCompleted);

        Debug.Log("Traveler challenge complete!");
        uiManager.RefreshUI();
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double r = 6371e3;
        double phi1 = lat1 * Math.PI / 180;
        double phi2 = lat2 * Math.PI / 180;
        double deltaPhi = (lat2 - lat1) * Math.PI / 180;
        double deltaLambda = (lon2 - lon1) * Math.PI / 180;

        double a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                   Math.Cos(phi1) * Math.Cos(phi2) *
                   Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return r * c;
    }

    #endregion

    #region Nested Helper Classes

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

    [System.Serializable]
    public class GooglePlacesResponse
    {
        public GooglePlaceResult[] results;
    }

    [System.Serializable]
    public class GooglePlaceResult
    {
        public string name;
        public PlaceGeometry geometry;
    }

    [System.Serializable]
    public class PlaceGeometry
    {
        public PlaceLocation location;
    }

    [System.Serializable]
    public class PlaceLocation
    {
        public double lat;
        public double lng;
    }

    #endregion
}