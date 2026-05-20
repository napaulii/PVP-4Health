using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloudScroller : MonoBehaviour
{
    [Header("Cloud Sprites")]
    [Tooltip("Drag your cloud sprite prefabs here")]
    public GameObject[] cloudPrefabs;

    [Header("Strip Settings")]
    [Tooltip("How many clouds to spawn")]
    public int cloudCount = 8;
    [Tooltip("Fallback width if no RectTransform is found")]
    public float stripWidth = 200f;
    [Tooltip("How spread out clouds are vertically around the strip center")]
    public float verticalSpread = 0.4f;

    [Header("Scroll Settings")]
    [Tooltip("Speed the strip moves horizontally")]
    public float scrollSpeed = 1.5f;

    [Header("Scale Pulse Settings")]
    [Tooltip("How much clouds scale up/down (0.05 = 5%)")]
    public float scalePulseAmount = 0.08f;
    [Tooltip("How fast the scale pulses")]
    public float scalePulseSpeed = 0.8f;

    // Internal
    private List<CloudData> clouds = new List<CloudData>();
    private float stripX = 0f;
    private float halfStrip;

    private struct CloudData
    {
        public GameObject obj;
        public float localX;
        public float baseY;
        public float baseScale;
        public float pulseOffset;
    }

    void Start()
    {
        // Use the actual RectTransform width as the strip boundary
        RectTransform rt = GetComponent<RectTransform>();
        halfStrip = rt != null ? rt.rect.width / 2f : stripWidth / 2f;

        if (cloudPrefabs == null || cloudPrefabs.Length == 0)
        {
            Debug.LogError("CloudScroller: No cloud prefabs assigned!");
            return;
        }

        SpawnClouds();
    }

    void SpawnClouds()
    {
        float fullWidth = halfStrip * 2f;
        float spacing = fullWidth / cloudCount;

        for (int i = 0; i < cloudCount; i++)
        {
            GameObject prefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];

            float localX = -halfStrip + spacing * i + Random.Range(-spacing * 0.3f, spacing * 0.3f);
            float localY = Random.Range(-verticalSpread, verticalSpread);
            float baseScale = Random.Range(0.7f, 1.3f);

            GameObject obj = Instantiate(prefab, transform);
            obj.transform.localPosition = new Vector3(localX, localY, 0f);
            obj.transform.localScale = Vector3.one * baseScale;

            clouds.Add(new CloudData
            {
                obj = obj,
                localX = localX,
                baseY = localY,
                baseScale = baseScale,
                pulseOffset = Random.Range(0f, Mathf.PI * 2f)
            });
        }
    }

    void Update()
    {
        stripX -= scrollSpeed * Time.deltaTime;

        // Wrap the strip offset at the real edges
        if (stripX < -halfStrip) stripX += halfStrip * 2f;

        float time = Time.time;

        for (int i = 0; i < clouds.Count; i++)
        {
            CloudData c = clouds[i];
            if (c.obj == null) continue;

            float worldX = c.localX + stripX;

            // Wrap each cloud at the real strip edges
            if (worldX < -halfStrip) worldX += halfStrip * 2f;
            if (worldX > halfStrip) worldX -= halfStrip * 2f;

            c.obj.transform.localPosition = new Vector3(worldX, c.baseY, 0f);

            // Scale pulse
            float pulse = Mathf.Sin(time * scalePulseSpeed + c.pulseOffset) * scalePulseAmount;
            float s = c.baseScale + pulse;
            c.obj.transform.localScale = new Vector3(s, s, s);
        }
    }
}