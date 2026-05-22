using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android; // Required for Android permissions
#endif

public class GPSManager : MonoBehaviour
{
    public static GPSManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
#if UNITY_ANDROID
        // Prompt for permission. If already granted, start the sensor.
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }
        else
        {
            Input.location.Start(10f, 10f);
        }
#else
        Input.location.Start(10f, 10f);
#endif
    }

    /// <summary>
    /// Instantly returns the current GPS coordinates, or null if the sensor is still warming up.
    /// </summary>
    public (double lat, double lng)? GetLocation()
    {
#if UNITY_EDITOR
        return (40.785091, -73.968285);
#endif

        Debug.LogError($"[GPSManager] Status: {Input.location.status} | EnabledByUser: {Input.location.isEnabledByUser}");

        if (Input.location.status == LocationServiceStatus.Running)
        {
            return (Input.location.lastData.latitude, Input.location.lastData.longitude);
        }

        // Safe restart logic: only executes on-demand when clicked, preventing main-thread lag
#if UNITY_ANDROID
        if (Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            if (Input.location.status == LocationServiceStatus.Stopped || Input.location.status == LocationServiceStatus.Failed)
            {
                Input.location.Start(10f, 10f);
            }
        }
#endif

        return null;
    }
}