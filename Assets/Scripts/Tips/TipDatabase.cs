// TipDatabase.cs
using UnityEngine;
using System.IO; // Required for file operations
using System.Collections.Generic;

public class TipDatabase : MonoBehaviour
{
    public static TipDatabase instance;

    private List<Tip> allTips = new List<Tip>();
    private int lastTipIndex = -1;

    void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadTips();
    }

    private void LoadTips()
    {
        // Path to the JSON file in the StreamingAssets folder
        string path = Path.Combine(Application.streamingAssetsPath, "tips.json");

        if (File.Exists(path))
        {
            string jsonContent = File.ReadAllText(path);

            // Deserialize the JSON into our C# classes
            TipList loadedTips = JsonUtility.FromJson<TipList>(jsonContent);

            // Add the loaded tips to our main list
            allTips.AddRange(loadedTips.tips);

            Debug.Log(allTips.Count + " tips loaded successfully from the database.");
        }
        else
        {
            Debug.LogError("Database file not found at: " + path);
        }
    }

    public string GetRandomTip()
    {
        if (allTips.Count == 0)
        {
            return "No tips were found in the database.";
        }

        int randomIndex;
        if (allTips.Count > 1)
        {
            do
            {
                randomIndex = Random.Range(0, allTips.Count);
            } while (randomIndex == lastTipIndex);
        }
        else
        {
            randomIndex = 0;
        }

        lastTipIndex = randomIndex;
        return allTips[randomIndex].tipText;
    }
}