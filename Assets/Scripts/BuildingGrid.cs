using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class BuildingGrid : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 4;
    public int height = 4;

    [Header("Placement Settings")]
    public float objectScale = 0.2f;

    [Header("References")]
    [SerializeField] private Camera gridCamera;
    [SerializeField] private Material ghostMaterial;

    [System.Serializable]
    public struct PlaceableItem
    {
        public Item.ItemType itemType;
        public GameObject prefab;
    }

    [Header("Placeable Items")]
    public PlaceableItem[] placeableItems;

    //Internal state 
    private Grid grid;
    private bool isPlacementMode = false;

    private GameObject ghostObject;
    private GameObject objectToPlace;
    private BuildingData currentData;
    private Vector3 currentSnappedPos;

    private Dictionary<Vector2Int, GameObject> occupied = new Dictionary<Vector2Int, GameObject>();

    // Lifecycle 

    private void OnEnable() => EnhancedTouchSupport.Enable();
    private void OnDisable() => EnhancedTouchSupport.Disable();

    void Awake() => grid = GetComponent<Grid>();

    void Start()
    {
        if (gridCamera == null) gridCamera = Camera.main;
    }

    void Update()
    {
        if (!isPlacementMode) return;
        UpdateGhost();
        HandleInput();
    }

    // Public API 

    /// <summary>Call from Shop after purchase to begin placement mode.</summary>
    public bool StartPlacementItem(Item.ItemType itemType)
    {
        Debug.Log($"[BuildingGrid] StartPlacementItem called for {itemType}");
        foreach (var item in placeableItems)
        {
            Debug.Log($"[BuildingGrid] Checking {item.itemType} vs {itemType}");
            if (item.itemType != itemType) continue;
            if (item.prefab == null)
            {
                Debug.LogError($"[BuildingGrid] Prefab for {itemType} is null!");
                return false;
            }
            objectToPlace = item.prefab;
            currentData = null;
            isPlacementMode = true;
            Debug.Log($"[BuildingGrid] Placement mode ON for {itemType}");
            SpawnGhost();
            return true;
        }
        Debug.LogError($"[BuildingGrid] No prefab found for {itemType}.");
        return false;
    }

    public bool StartPlacementBuilding(GameObject prefab, BuildingData data)
    {
        if (prefab == null) { Debug.LogError("[BuildingGrid] Prefab is null!"); return false; }
        objectToPlace = prefab;
        currentData = data;  // can be null for single tile
        isPlacementMode = true;
        SpawnGhost();
        return true;
    }

    public void StopPlacement()
    {
        isPlacementMode = false;
        if (ghostObject != null) ghostObject.SetActive(false);
    }

    public bool InBounds(Vector2Int cell) =>
        cell.x >= 0 && cell.x < width &&
        cell.y >= 0 && cell.y < height;

    //Ghost 

    void SpawnGhost()
    {
        if (ghostObject != null) Destroy(ghostObject);

        ghostObject = Instantiate(objectToPlace);
        ghostObject.transform.localScale = Vector3.one * objectScale;

        // Disable colliders so ghost doesn't block raycasts
        foreach (var col in ghostObject.GetComponentsInChildren<Collider>())
            col.enabled = false;

        if (ghostMaterial != null)
            foreach (var r in ghostObject.GetComponentsInChildren<Renderer>())
                r.material = ghostMaterial;
    }

    void UpdateGhost()
    {
        if (ghostObject == null) { Debug.Log("[Grid] ghostObject is null"); return; }

        Vector2 screenPos = GetScreenPos();
        if (screenPos == Vector2.negativeInfinity) { ghostObject.SetActive(false); return; }

        Ray ray = gridCamera.ScreenPointToRay(screenPos);

        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("[Grid] Raycast missed nothing hit");
            ghostObject.SetActive(false);
            return;
        }

        Debug.Log($"[Grid] Hit: {hit.collider.gameObject.name} | Tag: {hit.collider.tag}");

        if (!hit.collider.CompareTag("GridSurface"))
        {
            ghostObject.SetActive(false);
            return;
        }

        Vector3Int cell3 = grid.WorldToCell(hit.point);
        currentSnappedPos = grid.CellToWorld(cell3) + grid.cellSize * 0.5f;
        Debug.Log($"[Grid] Snapped to cell: {cell3} world: {currentSnappedPos}");

        bool valid = CanPlaceHere(cell3);
        SetGhostColor(valid
            ? new Color(1f, 1f, 1f, 0.5f)
            : new Color(1f, 0f, 0f, 0.5f));

        ghostObject.SetActive(true);
        ghostObject.transform.position = currentSnappedPos;
    }

    void SetGhostColor(Color color)
    {
        if (ghostObject == null) return;
        foreach (var r in ghostObject.GetComponentsInChildren<Renderer>())
            r.material.color = color;
    }

    //Input

    void HandleInput()
    {
        if (!ghostObject.activeSelf) return;

        // Ignore taps on UI
        var eventData = new PointerEventData(EventSystem.current)
        { position = GetScreenPos() };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var r in results)
            if (r.gameObject.GetComponent<UnityEngine.UI.Button>() != null) return;

        bool tapped = (Touch.activeTouches.Count > 0
                        && Touch.activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Began)
                   || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (tapped) PlaceObject();
    }

    //Placement 

    void PlaceObject()
    {
        Vector3Int cell3 = grid.WorldToCell(currentSnappedPos);
        if (!CanPlaceHere(cell3)) return;

        List<Vector2Int> cells = GetLocatorCells(cell3);

        GameObject placed = Instantiate(objectToPlace, currentSnappedPos, transform.rotation);
        placed.transform.localScale = Vector3.one * objectScale;
        placed.transform.SetParent(transform);

        foreach (var cell in cells)
            occupied[cell] = placed;

        StopPlacement();
    }

    /// <summary>Used by FortressPlacement to place directly without input mode.</summary>
    public bool TryPlace(GameObject prefab, BuildingData data, Vector3 worldPos)
    {
        Vector3Int cell3 = grid.WorldToCell(worldPos);
        List<Vector2Int> cells = GetLocatorCells(cell3, prefab, data);

        foreach (var cell in cells)
            if (!InBounds(cell) || occupied.ContainsKey(cell)) return false;

        Vector3 snapped = grid.CellToWorld(cell3) + grid.cellSize * 0.5f;
        GameObject obj = Instantiate(prefab, snapped, Quaternion.identity, transform);

        foreach (var cell in cells)
            occupied[cell] = obj;

        return true;
    }

    public void Remove(GameObject building)
    {
        var toRemove = new List<Vector2Int>();
        foreach (var kvp in occupied)
            if (kvp.Value == building) toRemove.Add(kvp.Key);
        foreach (var cell in toRemove)
            occupied.Remove(cell);
        Destroy(building);
    }

    //Helpers

    bool CanPlaceHere(Vector3Int origin)
    {
        foreach (var cell in GetLocatorCells(origin))
        {
            if (!InBounds(cell) || occupied.ContainsKey(cell)) return false;
        }
        return true;
    }

    // Reads locators from currentData (used during placement mode)
    List<Vector2Int> GetLocatorCells(Vector3Int origin)
        => GetLocatorCells(origin, objectToPlace, currentData);

    // Reads locators from provided data (used by TryPlace)
    List<Vector2Int> GetLocatorCells(Vector3Int origin, GameObject prefab, BuildingData data)
    {
        var cells = new List<Vector2Int>();

        if (data != null && data.tileLocatorOffsets != null && data.tileLocatorOffsets.Length > 0)
        {
            foreach (Vector3 offset in data.tileLocatorOffsets)
            {
                Vector3Int cell = grid.WorldToCell(
                    grid.CellToWorld(origin) + offset);
                cells.Add(new Vector2Int(cell.x, cell.y));
            }
        }
        else
        {
            // No locators — single tile
            cells.Add(new Vector2Int(origin.x, origin.y));
        }

        return cells;
    }

    Vector2 GetScreenPos()
    {
        if (Touch.activeTouches.Count > 0) return Touch.activeTouches[0].screenPosition;
        if (Mouse.current != null) return Mouse.current.position.ReadValue();
        return Vector2.negativeInfinity;
    }

    public Vector3 GetFootprintCenter(Vector3Int origin, int sizeX, int sizeZ)
    {
        Vector3 a = grid.CellToWorld(origin);
        Vector3 b = grid.CellToWorld(origin + new Vector3Int(sizeX, 0, sizeZ));
        return (a + b) * 0.5f;
    }
}