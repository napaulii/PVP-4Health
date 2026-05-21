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

    [Header("Cloud Size")]
    [Tooltip("Minimum cloud scale")]
    public float minCloudScale = 0.7f;
    [Tooltip("Maximum cloud scale")]
    public float maxCloudScale = 1.3f;

    [Header("Scroll Settings")]
    [Tooltip("Speed the strip moves horizontally")]
    public float scrollSpeed = 1.5f;
    [Tooltip("Checked = scroll right, Unchecked = scroll left")]
    public bool scrollRight = false;

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
            float baseScale = Random.Range(minCloudScale, maxCloudScale);

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
        float direction = scrollRight ? 1f : -1f;
        stripX += direction * scrollSpeed * Time.deltaTime;

        // Wrap strip offset
        if (stripX < -halfStrip) stripX += halfStrip * 2f;
        if (stripX > halfStrip) stripX -= halfStrip * 2f;

        float time = Time.time;

        for (int i = 0; i < clouds.Count; i++)
        {
            CloudData c = clouds[i];
            if (c.obj == null) continue;

            float worldX = c.localX + stripX;

            if (worldX < -halfStrip) worldX += halfStrip * 2f;
            if (worldX > halfStrip) worldX -= halfStrip * 2f;

            c.obj.transform.localPosition = new Vector3(worldX, c.baseY, 0f);

            float pulse = Mathf.Sin(time * scalePulseSpeed + c.pulseOffset) * scalePulseAmount;
            float s = c.baseScale + pulse;
            c.obj.transform.localScale = new Vector3(s, s, s);
        }
    }
}