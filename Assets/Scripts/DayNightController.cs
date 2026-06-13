using UnityEngine;
using UnityEngine.UI;
using System;

public class DayNightController : MonoBehaviour
{
    [Header("Background Material")]
    [SerializeField] private Material skyMaterial;

    [Header("UI Elements to tint (optional)")]
    [SerializeField] private Image[] uiElements;

    [Header("Directional Light")]
    [SerializeField] private Light directionalLight;

    [System.Serializable]
    public struct TimeOfDayColors
    {
        [Range(0f, 24f)] public float hour;
        public Color skyTop;
        public Color skyBottom;
        public Color uiTint;
        public float lightIntensity;
        public Color lightColor;
        public Vector2 lightRotation;
    }

    [Header("Color Keyframes (sort by hour)")]
    [SerializeField]
    private TimeOfDayColors[] keyframes = new TimeOfDayColors[]
    {
        new TimeOfDayColors {
            hour           = 0f,
            lightRotation  = new Vector2(-30f, 180f),
            lightIntensity = 0.05f,
            lightColor     = new Color(0.20f, 0.20f, 0.40f),
            skyTop         = new Color(0.02f, 0.02f, 0.10f),
            skyBottom      = new Color(0.05f, 0.05f, 0.18f),
            uiTint         = new Color(0.15f, 0.15f, 0.30f)
        },
        new TimeOfDayColors {
            hour           = 6f,
            lightRotation  = new Vector2(10f, 90f),
            lightIntensity = 0.6f,
            lightColor     = new Color(1.00f, 0.70f, 0.40f),
            skyTop         = new Color(0.95f, 0.50f, 0.20f),
            skyBottom      = new Color(1.00f, 0.80f, 0.60f),
            uiTint         = new Color(1.00f, 0.75f, 0.50f)
        },
        new TimeOfDayColors {
            hour           = 12f,
            lightRotation  = new Vector2(75f, 170f),
            lightIntensity = 1.0f,
            lightColor     = new Color(1.00f, 0.98f, 0.90f),
            skyTop         = new Color(0.45f, 0.85f, 1.00f),
            skyBottom      = new Color(1.00f, 1.00f, 1.00f),
            uiTint         = new Color(0.90f, 0.95f, 1.00f)
        },
        new TimeOfDayColors {
            hour           = 18f,
            lightRotation  = new Vector2(10f, 270f),
            lightIntensity = 0.4f,
            lightColor     = new Color(1.00f, 0.60f, 0.30f),
            skyTop         = new Color(0.60f, 0.20f, 0.50f),
            skyBottom      = new Color(0.95f, 0.55f, 0.30f),
            uiTint         = new Color(0.90f, 0.60f, 0.50f)
        }
    };

    [Header("Update interval in seconds")]
    [SerializeField] private float updateInterval = 60f;

    private float _timer;

    void Start() => Apply();

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= updateInterval)
        {
            _timer = 0f;
            Apply();
        }
    }

    void Apply()
    {
        float currentHour = (float)DateTime.Now.Hour
                          + (float)DateTime.Now.Minute / 60f
                          + (float)DateTime.Now.Second / 3600f;

        Color top, bottom, ui, lightCol;
        float intensity;
        Vector2 lightRot;

        SampleColors(currentHour, out top, out bottom, out ui,
                     out intensity, out lightCol, out lightRot);

        if (skyMaterial != null)
        {
            skyMaterial.SetColor("_SkyTop", top);
            skyMaterial.SetColor("_SkyBottom", bottom);
        }

        foreach (var img in uiElements)
            if (img != null) img.color = ui;

        if (directionalLight != null)
        {
            directionalLight.color = lightCol;
            directionalLight.intensity = intensity;
            directionalLight.transform.rotation =
                Quaternion.Euler(lightRot.x, lightRot.y, 0f);
        }
    }

    void SampleColors(float hour, out Color top, out Color bottom, out Color ui,
                      out float intensity, out Color lightCol, out Vector2 lightRot)
    {
        // Default fallback — satisfies the compiler if keyframes is empty
        top = Color.white;
        bottom = Color.white;
        ui = Color.white;
        intensity = 1f;
        lightCol = Color.white;
        lightRot = Vector2.zero;

        if (keyframes == null || keyframes.Length == 0) return;

        int count = keyframes.Length;
        int nextIndex = 0;

        for (int i = 0; i < count; i++)
        {
            if (keyframes[i].hour > hour)
            {
                nextIndex = i;
                break;
            }
            if (i == count - 1)
                nextIndex = 0;
        }

        int prevIndex = (nextIndex - 1 + count) % count;

        float prevHour = keyframes[prevIndex].hour;
        float nextHour = keyframes[nextIndex].hour;

        if (nextHour <= prevHour) nextHour += 24f;
        if (hour < prevHour) hour += 24f;

        float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(prevHour, nextHour, hour));

        top = Color.Lerp(keyframes[prevIndex].skyTop, keyframes[nextIndex].skyTop, t);
        bottom = Color.Lerp(keyframes[prevIndex].skyBottom, keyframes[nextIndex].skyBottom, t);
        ui = Color.Lerp(keyframes[prevIndex].uiTint, keyframes[nextIndex].uiTint, t);
        intensity = Mathf.Lerp(keyframes[prevIndex].lightIntensity, keyframes[nextIndex].lightIntensity, t);
        lightCol = Color.Lerp(keyframes[prevIndex].lightColor, keyframes[nextIndex].lightColor, t);

        float rotX = Mathf.LerpAngle(keyframes[prevIndex].lightRotation.x,
                                      keyframes[nextIndex].lightRotation.x, t);
        float rotY = Mathf.LerpAngle(keyframes[prevIndex].lightRotation.y,
                                      keyframes[nextIndex].lightRotation.y, t);
        lightRot = new Vector2(rotX, rotY);
    }

#if UNITY_EDITOR
    [ContextMenu("Preview Current Time")]
    void PreviewNow() => Apply();
#endif
}