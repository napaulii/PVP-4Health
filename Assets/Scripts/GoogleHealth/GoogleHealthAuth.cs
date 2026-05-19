using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

/// <summary>
/// Fetches health data (steps, heart rate) from the Google Fitness REST API.
/// Authentication is handled by Supabase (LoginManager) — this script uses
/// the access token from the active Supabase session, not its own OAuth flow.
/// </summary>
public class GoogleHealthAuth : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    private string _accessToken;
    private DateTime _tokenExpiresAt;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiresAt;

    // -------------------------------------------------------------------------
    // Initialise from Supabase session
    // -------------------------------------------------------------------------

    /// <summary>
    /// Call this after Supabase login completes to give this script the access token.
    /// In HabitManager.cs: googleHealth.InitFromSupabaseSession();
    /// </summary>
    public void InitFromSupabaseSession()
    {
        var session = SupabaseManager.Instance.Auth.CurrentSession;
        if (session == null)
        {
            Debug.LogWarning("[GoogleHealth] No active Supabase session found.");
            return;
        }

        _accessToken = session.AccessToken;
        _tokenExpiresAt = DateTime.UtcNow.AddSeconds(session.ExpiresIn - 60);

        Debug.Log("[GoogleHealth] Initialised from Supabase session.");
    }

    // -------------------------------------------------------------------------
    // Fetch Steps
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fetches total step count for a date range.
    /// startTimeMs / endTimeMs are Unix timestamps in milliseconds.
    /// </summary>
    public IEnumerator FetchSteps(long startTimeMs, long endTimeMs, Action<int> onResult)
    {
        if (!IsAuthenticated)
        {
            Debug.LogError("[GoogleHealth] Cannot fetch steps — not authenticated.");
            onResult?.Invoke(-1);
            yield break;
        }

        string url = "https://www.googleapis.com/fitness/v1/users/me/dataset:aggregate";

        string body = $@"{{
            ""aggregateBy"": [{{
                ""dataTypeName"": ""com.google.step_count.delta"",
                ""dataSourceId"": ""derived:com.google.step_count.delta:com.google.android.gms:estimated_steps""
            }}],
            ""bucketByTime"": {{ ""durationMillis"": {endTimeMs - startTimeMs} }},
            ""startTimeMillis"": {startTimeMs},
            ""endTimeMillis"": {endTimeMs}
        }}";

        using var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + _accessToken);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            int steps = ParseStepsFromResponse(request.downloadHandler.text);
            Debug.Log($"[GoogleHealth] Steps fetched: {steps}");
            onResult?.Invoke(steps);
        }
        else
        {
            Debug.LogError("[GoogleHealth] Steps fetch failed: " + request.downloadHandler.text);
            onResult?.Invoke(-1);
        }
    }

    // -------------------------------------------------------------------------
    // Fetch Heart Rate
    // -------------------------------------------------------------------------

    public IEnumerator FetchHeartRate(long startTimeMs, long endTimeMs, Action<float> onResult)
    {
        if (!IsAuthenticated)
        {
            Debug.LogError("[GoogleHealth] Cannot fetch heart rate — not authenticated.");
            onResult?.Invoke(-1f);
            yield break;
        }

        string url = "https://www.googleapis.com/fitness/v1/users/me/dataset:aggregate";

        string body = $@"{{
            ""aggregateBy"": [{{
                ""dataTypeName"": ""com.google.heart_rate.bpm""
            }}],
            ""bucketByTime"": {{ ""durationMillis"": {endTimeMs - startTimeMs} }},
            ""startTimeMillis"": {startTimeMs},
            ""endTimeMillis"": {endTimeMs}
        }}";

        using var request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + _accessToken);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            float bpm = ParseHeartRateFromResponse(request.downloadHandler.text);
            Debug.Log($"[GoogleHealth] Avg heart rate: {bpm} bpm");
            onResult?.Invoke(bpm);
        }
        else
        {
            Debug.LogError("[GoogleHealth] Heart rate fetch failed: " + request.downloadHandler.text);
            onResult?.Invoke(-1f);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    public static (long start, long end) TodayTimestamps()
    {
        var today = DateTime.UtcNow.Date;
        long start = new DateTimeOffset(today).ToUnixTimeMilliseconds();
        long end = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
        return (start, end);
    }

    private int ParseStepsFromResponse(string json)
    {
        int total = 0;
        int idx = 0;
        while ((idx = json.IndexOf("\"intVal\":", idx)) != -1)
        {
            idx += 9;
            int end = json.IndexOf(',', idx);
            if (end == -1) end = json.IndexOf('}', idx);
            if (end != -1 && int.TryParse(json.Substring(idx, end - idx).Trim(), out int val))
                total += val;
        }
        return total;
    }

    private float ParseHeartRateFromResponse(string json)
    {
        int idx = json.IndexOf("\"fpVal\":");
        if (idx == -1) return -1f;
        idx += 8;
        int end = json.IndexOf(',', idx);
        if (end == -1) end = json.IndexOf('}', idx);
        if (end != -1 && float.TryParse(json.Substring(idx, end - idx).Trim(),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float val))
            return val;
        return -1f;
    }
}