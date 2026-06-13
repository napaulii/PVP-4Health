using UnityEngine;
using System.Collections.Generic;

public class FortressPlacement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Grid grid;
    [SerializeField] private ObjectsDatabaseSO database;
    [SerializeField] private ObjectPlacer objectPlacer;

    [Header("Fortress IDs in ObjectsDatabaseSO (one per level)")]
    [SerializeField] private int[] fortressIDs = { 100, 101, 102, 103 };

    [Header("Grid center cell to place fortress on")]
    [SerializeField] private Vector3Int centerCell = new Vector3Int(1, 0, 1);

    private GridData _fortressGrid = new GridData();
    private int _placedObjectIndex = -1;
    private int _currentLevel = -1;

    void OnEnable() => FortressUpdateScript.OnLevelReached += OnLevelReached;
    void OnDisable() => FortressUpdateScript.OnLevelReached -= OnLevelReached;

    void Start()
    {

    }

    void OnLevelReached(int newLevel)
    {
        PlaceFortressForLevel(newLevel);
    }

    void PlaceFortressForLevel(int level)
    {
        if (level == _currentLevel) return;
        if (level < 0 || level >= fortressIDs.Length) return;

        int id = fortressIDs[level];
        int dataIndex = database.objectsData.FindIndex(d => d.ID == id);
        if (dataIndex < 0)
        {
            Debug.LogError($"[Fortress] No database entry for ID {id}");
            return;
        }

        ObjectData data = database.objectsData[dataIndex];

        // Remove old fortress from grid data and scene
        if (_placedObjectIndex >= 0)
        {
            _fortressGrid.RemoveObjectAt(centerCell);
            objectPlacer.RemoveObjectAt(_placedObjectIndex);
        }

        // Place new fortress
        Vector3 worldPos = grid.CellToWorld(centerCell);
        _placedObjectIndex = objectPlacer.PlaceObject(data.Prefab, worldPos);

        _fortressGrid.AddObjectAt(centerCell, data.Size, data.ID, _placedObjectIndex);

        _currentLevel = level;
        Debug.Log($"[Fortress] Placed level {level} ({data.Name}) at cell {centerCell}");
    }

    // Call this from PlacementSystem to check if a cell is blocked by the fortress
    public bool IsCellOccupied(Vector3Int cell, Vector2Int size)
    {
        return !_fortressGrid.CanPlaceObejctAt(cell, size);
    }
}