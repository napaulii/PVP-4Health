using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private Grid grid;
    [SerializeField] private ObjectsDatabaseSO database;
    [SerializeField] private GameObject gridVisualization;
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip correctPlacementClip;
    [SerializeField] private AudioClip wrongPlacementClip;
    [SerializeField] private PreviewSystem preview;
    [SerializeField] private ObjectPlacer objectPlacer;
    [SerializeField] private SoundFeedback soundFeedback;
    [SerializeField] private FortressPlacement fortressPlacement;

    private GridData floorData;
    private GridData furnitureData;
    private bool _skipNextClick = false;

    private IBuildingState buildingState;

    private Vector3Int lastDetectedPosition = Vector3Int.zero;

    private void Start()
    {
        floorData = new GridData();
        furnitureData = new GridData();

        if (gridVisualization != null)
            gridVisualization.SetActive(false);
    }

    public void StartPlacement(int id)
    {
        Debug.Log($"[Placement] StartPlacement ID = {id}");
        StopPlacement();

        if (database == null) { Debug.LogError("Database is NULL"); return; }

        int index = database.objectsData.FindIndex(x => x.ID == id);
        if (index < 0)
        {
            Debug.LogError($"No object with ID {id} in database");
            return;
        }

        gridVisualization.SetActive(true);
        buildingState = new PlacementState(id, grid, preview, database,
                                           floorData, furnitureData,
                                           objectPlacer, soundFeedback);
        _skipNextClick = true;
        inputManager.OnClicked += PlaceStructure;
        inputManager.OnExit += StopPlacement;
    }

    public void StartRemoving()
    {
        StopPlacement();

        gridVisualization.SetActive(true);

        buildingState = new RemovingState(
            grid,
            preview,
            floorData,
            furnitureData,
            objectPlacer,
            soundFeedback
        );

        inputManager.OnClicked += PlaceStructure;
        inputManager.OnExit += StopPlacement;
    }

    private void PlaceStructure()
    {
        if (_skipNextClick) { _skipNextClick = false; return; }
        if (buildingState == null) return;
        if (inputManager.IsPointerOverUI()) return;

        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Debug.Log($"[Placement] Raw world pos: {mousePosition}");

        Vector3Int gridPosition = grid.WorldToCell(mousePosition);
        Debug.Log($"[Placement] Grid cell: {gridPosition}");

        Vector3 cellWorldPos = grid.CellToWorld(gridPosition);
        Debug.Log($"[Placement] Cell world pos: {cellWorldPos}");

        buildingState.OnAction(gridPosition);
    }

    public void StopPlacement()
    {
        if (buildingState == null)
            return;

        gridVisualization.SetActive(false);

        buildingState.EndState();

        inputManager.OnClicked -= PlaceStructure;
        inputManager.OnExit -= StopPlacement;

        buildingState = null;

        lastDetectedPosition = Vector3Int.zero;
    }

    private void Update()
    {
        if (buildingState == null)
            return;

        Vector3 mousePosition =
            inputManager.GetSelectedMapPosition();

        Vector3Int gridPosition =
            grid.WorldToCell(mousePosition);

        if (lastDetectedPosition != gridPosition)
        {
            Debug.Log($"[Placement] Preview moved {gridPosition}");

            lastDetectedPosition = gridPosition;
            buildingState.UpdateState(gridPosition);
        }
    }
}