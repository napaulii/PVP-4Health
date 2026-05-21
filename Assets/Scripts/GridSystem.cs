using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class GridSystem : MonoBehaviour
{
    private GameObject objectToPlace;
    public float gridSize = 1f;
    public float objectScale = 0.2f;
    public Transform fortress;
    public Transform gridPlane;

    private Vector3 currentSnappedPosition;
    private float bobFrequency = 3f;
    private float bobAmplitude = 0.15f;

    [SerializeField] private Camera islandCamera;
    [SerializeField] private Material ghostMaterial;

    [System.Serializable]
    public struct PlaceableItem
    {
        public Item.ItemType itemType;
        public GameObject prefab;
    }

    public PlaceableItem[] placeableItems;
    private bool isPlacementMode = false;

    private GameObject ghostObject;
    private HashSet<Vector3> occupiedPositions = new HashSet<Vector3>();

    private void OnEnable() => EnhancedTouchSupport.Enable();
    private void OnDisable() => EnhancedTouchSupport.Disable();

    private void Start()
    {
        if (islandCamera == null) islandCamera = Camera.main;
    }

    private void Update()
    {
        if (!isPlacementMode) return;
        UpdateGhostPosition();
        HandlePlacementInput();
    }

    void CreateGhostObject()
    {
        ghostObject = Instantiate(objectToPlace);
        ghostObject.transform.localScale = Vector3.one * objectScale;

        if (ghostObject.TryGetComponent<Collider>(out var col))
            col.enabled = false;

        if (ghostMaterial != null)
        {
            foreach (Renderer r in ghostObject.GetComponentsInChildren<Renderer>())
                r.material = ghostMaterial;
        }
    }

    void UpdateGhostPosition()
    {
        Vector2 screenPos = GetCurrentScreenPosition();
        if (screenPos == Vector2.negativeInfinity) { ghostObject.SetActive(false); return; }

        Ray ray = islandCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 point = hit.point;
            currentSnappedPosition = new Vector3(
                Mathf.Round(point.x / gridSize) * gridSize,
                Mathf.Round(point.y / gridSize) * gridSize,
                Mathf.Round(point.z / gridSize) * gridSize
            );

            float bob = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;

            ghostObject.SetActive(true);
            ghostObject.transform.position = currentSnappedPosition + Vector3.up * bob;
            ghostObject.transform.rotation = gridPlane.rotation;

            SetGhostColor(occupiedPositions.Contains(currentSnappedPosition)
                ? new Color(1f, 0f, 0f, 0.5f)
                : new Color(1f, 1f, 1f, 0.5f));
        }
        else
        {
            ghostObject.SetActive(false);
        }
    }

    void SetGhostColor(Color color)
    {
        foreach (Renderer r in ghostObject.GetComponentsInChildren<Renderer>())
            r.material.color = color;
    }

    void HandlePlacementInput()
    {
        if (!ghostObject.activeSelf) return;

        var eventData = new PointerEventData(EventSystem.current)
        { position = GetCurrentScreenPosition() };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var r in results)
            if (r.gameObject.GetComponent<UnityEngine.UI.Button>() != null) return;

        bool tapped = (Touch.activeTouches.Count > 0
                        && Touch.activeTouches[0].phase == UnityEngine.InputSystem.TouchPhase.Began)
                   || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        if (tapped) PlaceObject();
    }

    void PlaceObject()
    {
        if (!occupiedPositions.Contains(currentSnappedPosition))
        {
            GameObject placed = Instantiate(objectToPlace, currentSnappedPosition, gridPlane.rotation);
            placed.transform.localScale = Vector3.one * objectScale;
            placed.transform.SetParent(fortress);
            occupiedPositions.Add(currentSnappedPosition);
            StopPlacement();
        }
    }

    Vector2 GetCurrentScreenPosition()
    {
        if (Touch.activeTouches.Count > 0) return Touch.activeTouches[0].screenPosition;
        if (Mouse.current != null) return Mouse.current.position.ReadValue();
        return Vector2.negativeInfinity;
    }

    public bool StartPlacement(Item.ItemType itemType)
    {
        if (placeableItems == null || placeableItems.Length == 0)
        {
            Debug.LogError("[GridSystem] placeableItems array is empty! Assign prefabs in the Inspector.");
            return false;
        }

        foreach (var item in placeableItems)
        {
            if (item.itemType == itemType)
            {
                if (item.prefab == null)
                {
                    Debug.LogError($"[GridSystem] Prefab for {itemType} is null! Assign it in the Inspector.");
                    return false;
                }

                objectToPlace = item.prefab;
                isPlacementMode = true;

                if (ghostObject != null) Destroy(ghostObject);
                CreateGhostObject();
                return true;
            }
        }

        Debug.LogError($"[GridSystem] No prefab entry found for {itemType}. Add it to placeableItems in the Inspector.");
        return false;
    }

    public void StopPlacement()
    {
        isPlacementMode = false;
        if (ghostObject != null) ghostObject.SetActive(false);
    }
}