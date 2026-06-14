using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPlacer : MonoBehaviour
{
    [SerializeField] private List<GameObject> placedGameObjects = new();
    [SerializeField] private ObjectsDatabaseSO database;

    [Serializable]
    private struct PlacementRecord
    {
        public int id;
        public Vector3 position;
    }

    [Serializable]
    private class PlacementList
    {
        public List<PlacementRecord> records = new();
    }

    private List<PlacementRecord> _records = new();

    private const string SaveKey = "PlacedObjects";

    void Start() => LoadPlacements();

    public int PlaceObject(GameObject prefab, Vector3 position)
    {
        GameObject newObject = Instantiate(prefab);
        newObject.transform.position = position;
        placedGameObjects.Add(newObject);

        // Find ID from database
        int id = -1;
        if (database != null)
        {
            var entry = database.objectsData.Find(d => d.Prefab == prefab);
            if (entry != null) id = entry.ID;
        }

        _records.Add(new PlacementRecord { id = id, position = position });
        Save();

        return placedGameObjects.Count - 1;
    }

    internal void RemoveObjectAt(int gameObjectIndex)
    {
        if (placedGameObjects.Count <= gameObjectIndex
            || placedGameObjects[gameObjectIndex] == null)
            return;

        Destroy(placedGameObjects[gameObjectIndex]);
        placedGameObjects[gameObjectIndex] = null;

        if (gameObjectIndex < _records.Count)
            _records.RemoveAt(gameObjectIndex);

        Save();
    }

    private void Save()
    {
        var list = new PlacementList { records = _records };
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(list));
        PlayerPrefs.Save();
    }

    private void LoadPlacements()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return;
        if (database == null)
        {
            Debug.LogWarning("[ObjectPlacer] Database not assigned, cannot restore placements.");
            return;
        }

        string json = PlayerPrefs.GetString(SaveKey);
        PlacementList list = JsonUtility.FromJson<PlacementList>(json);

        if (list?.records == null) return;

        foreach (var record in list.records)
        {
            if (record.id < 0) continue;

            var entry = database.objectsData.Find(d => d.ID == record.id);
            if (entry == null || entry.Prefab == null)
            {
                Debug.LogWarning($"[ObjectPlacer] No prefab found for ID {record.id}");
                continue;
            }

            GameObject obj = Instantiate(entry.Prefab);
            obj.transform.position = record.position;
            placedGameObjects.Add(obj);
            _records.Add(record);
        }

        Debug.Log($"[ObjectPlacer] Restored {list.records.Count} placed objects.");
    }

    // Call this if you want to wipe all placements (e.g. reset button)
    public void ClearAll()
    {
        foreach (var obj in placedGameObjects)
            if (obj != null) Destroy(obj);

        placedGameObjects.Clear();
        _records.Clear();
        PlayerPrefs.DeleteKey(SaveKey);
    }
}