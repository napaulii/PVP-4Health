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
    private async Task CompleteChallengeAsync(UserChallenge uc)
    {
        // Simply complete the personal challenge and refresh the UI.
        // No group increments are needed here anymore since the group works on its own tab!
        uc.Status = "completed";
        await SupabaseManager.Instance.From<UserChallenge>().Update(uc);
        uiManager.RefreshUI();
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
            if (row != null)
            {
                row.targetDestinationText.text = $"Target: {uc.TargetName}";
                if (row.targetDestinationDistance != null)
                {
                    row.targetDestinationDistance.text = "Arrived!";
                }
            }
            Debug.LogError("Updating Challenge");
            _ = CompleteChallengeAsync(uc);
        }
        else
        {
            if (row != null)
            {
                row.targetDestinationText.text = $"Target: {uc.TargetName}";

                if (row.targetDestinationDistance != null)
                {
                    // Formats distance nicely based on how far away you are
                    if (distance >= 1000)
                    {
                        row.targetDestinationDistance.text = $"{distance / 1000f:F1} km away";
                    }
                    else
                    {
                        row.targetDestinationDistance.text = $"{distance:F0} m away";
                    }
                }
            }
        }
    }

    private async Task FetchNearbyPlaceAsync(double lat, double lng, UserChallenge uc)
    {
        string url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={lat},{lng}&radius=5000&type=tourist_attraction|museum|park&key={googleApiKey}";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        using (UnityWebRequest wwwGet = UnityWebRequest.Get(url))
        {
            var operation = wwwGet.SendWebRequest();
            while (!operation.isDone) await Task.Delay(100);

            if (wwwGet.result == UnityWebRequest.Result.Success)
            {
                GooglePlacesResponse response = JsonUtility.FromJson<GooglePlacesResponse>(wwwGet.downloadHandler.text);
                if (response?.results != null && response.results.Length > 0)
                {
                    var place = response.results[UnityEngine.Random.Range(0, response.results.Length)];

                    string cleanName = place.name;
                    if (cleanName.Contains(","))
                    {
                        cleanName = cleanName.Split(',')[0].Trim();
                    }

                    uc.TargetName = cleanName;
                    uc.TargetLatitude = place.geometry.location.lat;
                    uc.TargetLongitude = place.geometry.location.lng;

                    await SupabaseManager.Instance.From<UserChallenge>().Update(uc);
                    uiManager.RefreshUI();
                }
            }
        }
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
    /// <summary>
    /// Opens the native Google Maps app on the phone with walking directions to the target.
    /// </summary>
    public void OpenMapForTarget(double targetLat, double targetLng)
    {
        // Creates a Google Maps intent for walking directions to the target
        string url = $"https://www.google.com/maps/search/?api=1&query={targetLat},{targetLng}";

        Debug.Log($"[Map] Opening native map app to: {url}");
        Application.OpenURL(url);
    }
    #endregion
    #region Group Traveler Logic

    public async void AutoGenerateGroupTravelerLocation(GroupChallenge gc, GroupChallengeRowUI row)
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

        // Fetch Google Places
        string url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={coords.Value.lat},{coords.Value.lng}&radius=5000&type=tourist_attraction|museum|park&key={googleApiKey}";
        using (UnityWebRequest wwwGet = UnityWebRequest.Get(url))
        {
            var operation = wwwGet.SendWebRequest();
            while (!operation.isDone) await Task.Delay(100);

            if (wwwGet.result == UnityWebRequest.Result.Success)
            {
                GooglePlacesResponse response = JsonUtility.FromJson<GooglePlacesResponse>(wwwGet.downloadHandler.text);
                if (response?.results != null && response.results.Length > 0)
                {
                    var place = response.results[UnityEngine.Random.Range(0, response.results.Length)];
                    string cleanName = place.name.Contains(",") ? place.name.Split(',')[0].Trim() : place.name;

                    gc.TargetName = cleanName;
                    gc.TargetLatitude = place.geometry.location.lat;
                    gc.TargetLongitude = place.geometry.location.lng;

                    await SupabaseManager.Instance.From<GroupChallenge>().Update(gc);
                    if (row != null) row._uiManager.RefreshUI();
                }
            }
        }
    }

    public void ExecuteGroupTravelerChallenge(GroupChallenge gc, GroupChallengeRowUI row)
    {
        var coords = GPSManager.Instance != null ? GPSManager.Instance.GetLocation() : null;
        if (coords == null)
        {
            if (string.IsNullOrEmpty(gc.TargetName)) AutoGenerateGroupTravelerLocation(gc, row);
            else if (row != null) row.targetDestinationText.text = "GPS initializing. Please try again.";
            return;
        }

        if (string.IsNullOrEmpty(gc.TargetName))
        {
            AutoGenerateGroupTravelerLocation(gc, row);
            return;
        }

        double distance = CalculateDistance(coords.Value.lat, coords.Value.lng, gc.TargetLatitude ?? 0, gc.TargetLongitude ?? 0);

        if (distance <= arrivalThresholdMeters)
        {
            if (row != null)
            {
                row.targetDestinationText.text = $"Target: {gc.TargetName}";
                if (row.targetDestinationDistance != null) row.targetDestinationDistance.text = "Arrived!";
                row.checkLocationButton.interactable = false;
            }
            _ = CompleteGroupTravelerAsync(gc, row);
        }
        else
        {
            if (row != null)
            {
                row.targetDestinationText.text = $"Target: {gc.TargetName}";
                if (row.targetDestinationDistance != null)
                {
                    row.targetDestinationDistance.text = distance >= 1000 ? $"{distance / 1000f:F1} km away" : $"{distance:F0} m away";
                }
            }
        }
    }

    private async Task CompleteGroupTravelerAsync(GroupChallenge gc, GroupChallengeRowUI row)
    {
        GroupChallengeController groupCtrl = new GroupChallengeController();
        await groupCtrl.ProgressGroupTravelerAsync(gc); // Increments, clears location, updates DB
        if (row != null) row._uiManager.RefreshUI(); // Refreshes UI (which auto-generates the next location!)
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