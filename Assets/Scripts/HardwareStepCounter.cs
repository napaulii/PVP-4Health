using UnityEngine;
using UnityEngine.InputSystem; // Requires Unity's New Input System package

public class HardwareStepCounter : MonoBehaviour
{
    public static HardwareStepCounter Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Android 10+ requires runtime permission for physical activity recognition.
        // Fully qualified path is used here to bypass any namespace errors.
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.ACTIVITY_RECOGNITION"))
        {
            Debug.Log("[HardwareStep] Requesting Activity Recognition permission...");
            UnityEngine.Android.Permission.RequestUserPermission("android.permission.ACTIVITY_RECOGNITION");
        }
#endif

        // Enable the phone's built-in step counter hardware
        if (StepCounter.current != null)
        {
            InputSystem.EnableDevice(StepCounter.current);
            Debug.Log("[HardwareStep] Phone step sensor successfully initialized.");
        }
        else
        {
            Debug.LogWarning("[HardwareStep] This device does not have a physical step sensor.");
        }
    }

    /// <summary>
    /// Returns the total steps taken since the device was last turned on.
    /// </summary>
    public int GetTotalDeviceSteps()
    {
#if UNITY_EDITOR
        return 7500; // Simulated steps for Unity Editor playtesting
#endif

        if (StepCounter.current != null)
        {
            return StepCounter.current.stepCounter.ReadValue();
        }
        return 0;
    }
}