using UnityEngine;
using System.Collections.Generic;

public class PlatformSystem : MonoBehaviour
{
    [Header("Platform")]
    public GameObject platformPrefab;
    public LayerMask platformLayer;
    public int[] platformsUnlockedAtLevel = { 1, 2, 3 };

    [Header("Ghost Preview")]
    public Material ghostMaterial;       // semi-transparent material
    public Color validColor = new Color(0f, 1f, 0f, 0.4f);   // green
    public Color invalidColor = new Color(1f, 0f, 0f, 0.4f);   // red

    private int platformsAvailable = 0;
    private List<BuildingGrid> allGrids = new List<BuildingGrid>();

    // Ghost state
    private GameObject ghostObject;
    private bool isPlacingPlatform = false;
    private bool ghostIsValid = false;
    private Vector3 ghostSnapPos;

    void OnEnable() => FortressUpdateScript.OnLevelReached += HandleLevelUp;
    void OnDisable() => FortressUpdateScript.OnLevelReached -= HandleLevelUp;

    void Start()
    {
        int current = FortressUpdateScript.CurrentLevel;
        if (current < platformsUnlockedAtLevel.Length)
            platformsAvailable = platformsUnlockedAtLevel[current];

        foreach (var grid in FindObjectsByType<BuildingGrid>(FindObjectsSortMode.None))
            allGrids.Add(grid);
    }

    void Update()
    {
        if (!isPlacingPlatform) return;

        // Update ghost position every frame to follow pointer
        Vector3 worldPos = GetPointerWorldPos();
        ghostSnapPos = GetSnapPosition(worldPos);
        ghostIsValid = platformsAvailable > 0;

        // Move ghost
        if (ghostObject != null)
        {
            ghostObject.transform.position = ghostSnapPos;
            SetGhostColor(ghostIsValid ? validColor : invalidColor);
        }

        // Confirm placement on tap / click
        if (Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            ConfirmPlacement();
        }

        // Cancel on right click / back button
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
        }
    }

    //  Public: call this from your UI "Place Platform" button 
    public void BeginPlacingPlatform()
    {
        if (platformsAvailable <= 0)
        {
            Debug.Log("[Platform] None available.");
            return;
        }

        isPlacingPlatform = true;
        SpawnGhost();
    }

    //  Ghost

    void SpawnGhost()
    {
        if (ghostObject != null) Destroy(ghostObject);

        ghostObject = Instantiate(platformPrefab);

        // Disable colliders and scripts on ghost so it doesn't interfere
        foreach (var col in ghostObject.GetComponentsInChildren<Collider>())
            col.enabled = false;
        foreach (var mono in ghostObject.GetComponentsInChildren<MonoBehaviour>())
            mono.enabled = false;

        // Apply ghost material to all renderers
        foreach (var rend in ghostObject.GetComponentsInChildren<Renderer>())
        {
            var mats = new Material[rend.materials.Length];
            for (int i = 0; i < mats.Length; i++)
                mats[i] = ghostMaterial;
            rend.materials = mats;
        }

        SetGhostColor(validColor);
    }

    void SetGhostColor(Color color)
    {
        if (ghostObject == null) return;
        foreach (var rend in ghostObject.GetComponentsInChildren<Renderer>())
            foreach (var mat in rend.materials)
                mat.color = color;
    }

    void DestroyGhost()
    {
        if (ghostObject != null) Destroy(ghostObject);
        ghostObject = null;
    }

    //  Confirm / Cancel 
    void ConfirmPlacement()
    {
        if (!ghostIsValid) return;

        DestroyGhost();
        isPlacingPlatform = false;

        GameObject obj = Instantiate(platformPrefab, ghostSnapPos, Quaternion.identity);
        BuildingGrid grid = obj.GetComponentInChildren<BuildingGrid>();
        if (grid != null) allGrids.Add(grid);

        platformsAvailable--;
        Debug.Log($"[Platform] Placed. Remaining: {platformsAvailable}");
    }

    void CancelPlacement()
    {
        DestroyGhost();
        isPlacingPlatform = false;
    }

    //Snap logic 

    Vector3 GetSnapPosition(Vector3 worldPos)
    {
        Collider[] nearby = Physics.OverlapSphere(worldPos, 3f, platformLayer);

        float bestDist = float.MaxValue;
        Vector3 bestPos = worldPos;

        foreach (var col in nearby)
        {
            if (col.GetComponentInChildren<BuildingGrid>() == null) continue;

            Vector3 edge = col.ClosestPoint(worldPos);
            Vector3 direction = (worldPos - col.bounds.center).normalized;
            Vector3 size = platformPrefab.GetComponent<Collider>().bounds.size;
            Vector3 candidate = edge + direction * (size.x * 0.5f);

            float dist = Vector3.Distance(worldPos, candidate);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestPos = candidate;
            }
        }

        return bestPos;
    }

    // Pointer helper (works for both mouse and touch)

    Vector3 GetPointerWorldPos()
    {
        Vector3 screenPos = Input.touchCount > 0
            ? (Vector3)Input.GetTouch(0).position
            : Input.mousePosition;

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, platformLayer))
            return hit.point;

        // Fallback — raycast against XZ plane
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        if (plane.Raycast(ray, out float dist))
            return ray.GetPoint(dist);

        return Vector3.zero;
    }

    //  Place building (unchanged) 
    public bool TryPlaceBuilding(BuildingData data, Vector3 worldPos)
    {
        foreach (var grid in allGrids)
        {
            if (grid.TryPlace(data.prefab, data, worldPos))
                return true;
        }
        Debug.Log("[Platform] No valid grid cell at that position.");
        return false;
    }

    void HandleLevelUp(int newLevel)
    {
        if (newLevel >= platformsUnlockedAtLevel.Length) return;
        int prev = platformsUnlockedAtLevel[Mathf.Max(0, newLevel - 1)];
        int gained = platformsUnlockedAtLevel[newLevel] - prev;
        platformsAvailable += gained;
        Debug.Log($"[Platform] Level {newLevel}! +{gained} platform(s). Available: {platformsAvailable}");
    }

    public int GetAvailablePlatforms() => platformsAvailable;
}