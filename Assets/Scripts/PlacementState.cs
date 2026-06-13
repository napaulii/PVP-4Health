using System.Collections.Generic;
using UnityEngine;

public class PlacementState : IBuildingState
{
    private int selectedObjectIndex = -1;
    int ID;
    Grid grid;
    PreviewSystem previewSystem;
    ObjectsDatabaseSO database;
    GridData floorData;
    GridData furnitureData;
    ObjectPlacer objectPlacer;
    SoundFeedback soundFeedback;
    FortressPlacement fortressPlacement;

    public PlacementState(int iD,
                          Grid grid,
                          PreviewSystem previewSystem,
                          ObjectsDatabaseSO database,
                          GridData floorData,
                          GridData furnitureData,
                          ObjectPlacer objectPlacer,
                          SoundFeedback soundFeedback,
                          FortressPlacement fortressPlacement = null)
    {
        ID = iD;
        this.grid = grid;
        this.previewSystem = previewSystem;
        this.database = database;
        this.floorData = floorData;
        this.furnitureData = furnitureData;
        this.objectPlacer = objectPlacer;
        this.soundFeedback = soundFeedback;
        this.fortressPlacement = fortressPlacement;

        selectedObjectIndex = database.objectsData.FindIndex(data => data.ID == ID);
        if (selectedObjectIndex > -1)
        {
            previewSystem.StartShowingPlacementPreview(
                database.objectsData[selectedObjectIndex].Prefab,
                database.objectsData[selectedObjectIndex].Size);
        }
        else
            throw new System.Exception($"No object with ID {iD}");
    }

    public void EndState()
    {
        previewSystem.StopShowingPreview();
    }

    public void OnAction(Vector3Int gridPosition)
    {
        GridData selectedData = database.objectsData[selectedObjectIndex].ID == 0
            ? floorData : furnitureData;

        bool alreadyPlaced = selectedData.ContainsObjectType(
            database.objectsData[selectedObjectIndex].ID);
        Debug.Log($"[Placement] ID {database.objectsData[selectedObjectIndex].ID} already placed: {alreadyPlaced}");

        if (alreadyPlaced)
        {
            Debug.Log("[Placement] Blocked by ContainsObjectType");
            soundFeedback.PlaySound(SoundType.wrongPlacement);
            return;
        }

        if (selectedData.ContainsObjectType(database.objectsData[selectedObjectIndex].ID))
        {
            Debug.Log("[Placement] This item type is already placed.");
            soundFeedback.PlaySound(SoundType.wrongPlacement);
            return;
        }

        bool placementValidity = CheckPlacementValidity(gridPosition, selectedObjectIndex);
        if (!placementValidity)
        {
            soundFeedback.PlaySound(SoundType.wrongPlacement);
            return;
        }

        soundFeedback.PlaySound(SoundType.Place);

        int index = objectPlacer.PlaceObject(
            database.objectsData[selectedObjectIndex].Prefab,
            grid.CellToWorld(gridPosition));

        selectedData.AddObjectAt(
            gridPosition,
            database.objectsData[selectedObjectIndex].Size,
            database.objectsData[selectedObjectIndex].ID,
            index);

        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), false);
    }

    private bool CheckPlacementValidity(Vector3Int gridPosition, int selectedObjectIndex)
    {
        Vector2Int size = database.objectsData[selectedObjectIndex].Size;

        if (fortressPlacement != null && fortressPlacement.IsCellOccupied(gridPosition, size))
            return false;

        return furnitureData.CanPlaceObejctAt(gridPosition, size)
            && floorData.CanPlaceObejctAt(gridPosition, size);
    }

    public void UpdateState(Vector3Int gridPosition)
    {
        bool placementValidity = CheckPlacementValidity(gridPosition, selectedObjectIndex);
        previewSystem.UpdatePosition(grid.CellToWorld(gridPosition), placementValidity);
    }
}