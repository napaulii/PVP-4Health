using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class GPSManager : MonoBehaviour
{
    public static GPSManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public async Task<(double lat, double lng)?> GetCurrentLocationAsync()
    {
        // 1. If running inside the Unity Editor, return mock coordinates
#if UNITY_EDITOR
        // Fake coordinates (e.g., Central Park, New York)
        // You can change these to coordinates in your city to find local landmarks!
        double mockLatitude = 40.785091; 
        double mockLongitude = -73.968285;

        await Task.Delay(500); // Simulate sensor warm-up delay
        return (mockLatitude, mockLongitude);
#else

        // 2. If running on a real phone, use the actual hardware GPS
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("GPS is not enabled by the user.");
            return null;
        }

        Input.location.Start();

        int maxWait = 20;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            await Task.Delay(1000);
            maxWait--;
        }

        if (maxWait < 1 || Input.location.status == LocationServiceStatus.Failed)
        {
            Debug.LogWarning("Failed to initialize GPS location services.");
            Input.location.Stop();
            return null;
        }

        double latitude = Input.location.lastData.latitude;
        double longitude = Input.location.lastData.longitude;

        Input.location.Stop();
        return (latitude, longitude);
#endif
    }
}