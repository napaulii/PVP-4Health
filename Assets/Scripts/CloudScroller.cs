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

    [Tooltip("Total width of the cloud strip (should be wider than screen)")]
    public float stripWidth = 30f;

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

    private struct CloudData
    {
        public GameObject obj;
        public float localX;       // position along the strip
        public float baseY;        // vertical offset
        public float baseScale;    // random base scale
        public float pulseOffset;  // phase offset so clouds don't all pulse together
    }

    void Start()
    {
        if (cloudPrefabs == null || cloudPrefabs.Length == 0)
        {
            Debug.LogError("CloudScroller: No cloud prefabs assigned!");
            return;
        }

        SpawnClouds();
    }

    void SpawnClouds()
    {
        // Space clouds evenly with some random jitter
        float spacing = stripWidth / cloudCount;

        for (int i = 0; i < cloudCount; i++)
        {
            // Pick a random cloud prefab
            GameObject prefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];

            // Position: evenly spaced + jitter, centered so strip goes -half to +half
            float localX = -stripWidth / 2f + spacing * i + Random.Range(-spacing * 0.3f, spacing * 0.3f);
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
                pulseOffset = Random.Range(0f, Mathf.PI * 2f)  // random phase
            });
        }
    }

    void Update()
    {
        // Advance the strip
        stripX -= scrollSpeed * Time.deltaTime;

        // Wrap: when strip has moved one full cloud-spacing, reset and it's seamless
        float halfStrip = stripWidth / 2f;
        if (stripX < -halfStrip)
            stripX += stripWidth;

        float time = Time.time;

        for (int i = 0; i < clouds.Count; i++)
        {
            CloudData c = clouds[i];
            if (c.obj == null) continue;

            // Scroll position
            float worldX = c.localX + stripX;

            // Wrap individual cloud so it re-enters from the other side
            if (worldX < -halfStrip) worldX += stripWidth;
            if (worldX > halfStrip) worldX -= stripWidth;

            c.obj.transform.localPosition = new Vector3(worldX, c.baseY, 0f);

            // Scale pulse
            float pulse = Mathf.Sin(time * scalePulseSpeed + c.pulseOffset) * scalePulseAmount;
            float s = c.baseScale + pulse;
            c.obj.transform.localScale = new Vector3(s, s, s);
        }
    }
}