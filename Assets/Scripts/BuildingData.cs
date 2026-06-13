using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "BuildingData", menuName = "Game/Building Data")]
public class BuildingData : ScriptableObject
{
    public string buildingName;
    public GameObject prefab;
    public Vector3[] tileLocatorOffsets;

#if UNITY_EDITOR
    [ContextMenu("Read Locators From Prefab")]
    void ReadLocatorsFromPrefab()
    {
        if (prefab == null) { Debug.LogError("Assign the prefab first!"); return; }

        // Find all children named "TileLocator" anything
        var locators = new System.Collections.Generic.List<Vector3>();
        foreach (Transform child in prefab.GetComponentsInChildren<Transform>())
        {
            if (child.name.StartsWith("TileLocator"))
                locators.Add(child.localPosition);
        }

        tileLocatorOffsets = locators.ToArray();
        EditorUtility.SetDirty(this);
        Debug.Log($"[BuildingData] Read {locators.Count} locators from {prefab.name}");
    }
#endif
}