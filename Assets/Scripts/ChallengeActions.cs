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
    [SerializeField] private string googleApiKey = "YOUR_GOOGLE_PLACES_API_KEY";
    [SerializeField] private double arrivalThresholdMeters = 100.0;

    [Header("References")]
    public ChallengeUIManager uiManager;

    private UserController _userController = new UserController();

    public void ExecuteChallengeAction(UserChallenge uc, ChallengeRowUI row = null)
    {
        string challengeType = (uc.ChallengeData?.Type ?? "").ToLower();

        if (challengeType.Contains("meal"))
        {
            TakePhotoAndValidate(uc, row);
        }
        else if (challengeType.Contains("traveler"))
        {
            Debug.LogError("Starting HandleTravelerChallenge");
            HandleTravelerChallenge(uc, row);
        }
    }

    public async void AutoGenerateTravelerLocation(UserChallenge uc, ChallengeRowUI row = null)
    {
        var coords = GPSManager.Instance != null ? GPSManager.Instance.GetLocation() : null;

        if (coords == null)
        {
            if (row != null)
            {
                row.targetDestinationText.text = "GPS warming up. Tap button to search.";
                row.checkLocationButton.interactable = true;
                row.checkLocationButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Generate Location";
            }
            return;
        }

        if (row != null)
        {
            row.targetDestinationText.text = "Locating nearby landmark...";
            row.checkLocationButton.interactable = false;
        }

        await FetchNearbyPlaceAsync(coords.Value.lat, coords.Value.lng, uc);
    }

    #region Healthy Meal Logic

    private void TakePhotoAndValidate(UserChallenge uc, ChallengeRowUI row = null)
    {
        NativeCamera.TakePicture((path) =>
        {
            if (path != null) StartCoroutine(ProcessAndUpload(path, uc, row));
        }, 1024);
    }

    private IEnumerator ProcessAndUpload(string path, UserChallenge uc, ChallengeRowUI row = null)
    {
        byte[] imageBytes = System.IO.File.ReadAllBytes(path);
        string base64Image = System.Convert.ToBase64String(imageBytes);

        var requestData = new UploadRequest { imageBase64 = base64Image, userId = uc.UserId, challengeId = (int)uc.ChallengeId };
        string json = JsonUtility.ToJson(requestData);

        using (UnityWebRequest www = new UnityWebRequest(edgeFunctionUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            string token = SupabaseManager.Instance.Auth.CurrentSession?.AccessToken;
            if (string.IsNullOrEmpty(token)) yield break;
            www.SetRequestHeader("Authorization", "Bearer " + token);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AIResponse response = JsonUtility.FromJson<AIResponse>(www.downloadHandler.text);
                if (response.isHealthy)
                {
                    if (row != null) row.CloseDetails();
                    _ = CompleteChallengeAsync(uc);
                }
                else if (row != null)
                {
                    row.targetDestinationText.text = $"Rejected: {response.reason}";
                }
            }
        }
    }

    #endregion

    #region Traveler Logic (No Coroutines)

    private void HandleTravelerChallenge(UserChallenge uc, ChallengeRowUI row)
    {
        var coords = GPSManager.Instance != null ? GPSManager.Instance.GetLocation() : null;
        Debug.LogError("coords: " + coords);
        if (coords == null)
        {
            if (string.IsNullOrEmpty(uc.TargetName))
            {
                AutoGenerateTravelerLocation(uc, row);
            }
            else
            {
                if (row != null) row.targetDestinationText.text = "GPS initializing. Please try again.";
            }
            return;
        }

        if (string.IsNullOrEmpty(uc.TargetName))
        {
            AutoGenerateTravelerLocation(uc, row);
            return;
        }

        double distance = CalculateDistance(coords.Value.lat, coords.Value.lng, uc.TargetLatitude ?? 0, uc.TargetLongitude ?? 0);
        Debug.LogError("distance: " + distance);
        if (distance <= arrivalThresholdMeters)
        {
            if (row != null) row.targetDestinationText.text = "Arrived! Updating progress...";
            Debug.LogError("Updating Challenge");
            _ = CompleteChallengeAsync(uc);
        }
        else
        {
            if (row != null) row.targetDestinationText.text = $"Target: {uc.TargetName}\n({distance:F0}m away)";
        }
    }

    private async Task FetchNearbyPlaceAsync(double lat, double lng, UserChallenge uc)
    {
        string url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={lat},{lng}&radius=5000&type=tourist_attraction|museum|park&key={googleApiKey}";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            var operation = www.SendWebRequest();
            while (!operation.isDone) await Task.Delay(100);

            if (www.result == UnityWebRequest.Result.Success)
            {
                GooglePlacesResponse response = JsonUtility.FromJson<GooglePlacesResponse>(www.downloadHandler.text);
                if (response?.results != null && response.results.Length > 0)
                {
                    var place = response.results[UnityEngine.Random.Range(0, response.results.Length)];
                    uc.TargetName = place.name;
                    uc.TargetLatitude = place.geometry.location.lat;
                    uc.TargetLongitude = place.geometry.location.lng;

                    await SupabaseManager.Instance.From<UserChallenge>().Update(uc);
                    uiManager.RefreshUI();
                }
            }
        }
    }

    private async Task CompleteChallengeAsync(UserChallenge uc)
    {
        uc.Status = "completed";
        await SupabaseManager.Instance.From<UserChallenge>().Update(uc);
        uiManager.RefreshUI();
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double r = 6371e3;
        double phi1 = lat1 * Math.PI / 180, phi2 = lat2 * Math.PI / 180;
        double dPhi = (lat2 - lat1) * Math.PI / 180, dLon = (lon2 - lon1) * Math.PI / 180;

        double a = Math.Sin(dPhi / 2) * Math.Sin(dPhi / 2) +
                   Math.Cos(phi1) * Math.Cos(phi2) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return r * (2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
    }

    #endregion

    #region Helper Classes

    [System.Serializable] public class UploadRequest { public string imageBase64; public string userId; public int challengeId; }
    [System.Serializable] public class AIResponse { public bool isHealthy; public string reason; }
    [System.Serializable] public class GooglePlacesResponse { public GooglePlaceResult[] results; }
    [System.Serializable] public class GooglePlaceResult { public string name; public PlaceGeometry geometry; }
    [System.Serializable] public class PlaceGeometry { public PlaceLocation location; }
    [System.Serializable] public class PlaceLocation { public double lat; public double lng; }

    #endregion
}