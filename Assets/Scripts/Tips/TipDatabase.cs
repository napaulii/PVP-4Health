// TipDatabase.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Postgrest.Attributes;
using Postgrest.Models;
using SupabaseModels;

// --- Tip model matching your Supabase table ---
namespace SupabaseModels
{
    [Table("tips")]
    public class TipModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("tip_text")]
        public string Tip { get; set; }
    }
}

public class TipDatabase : MonoBehaviour
{
    public static TipDatabase instance;

    private List<string> allTips = new List<string>();
    private int lastTipIndex = -1;
    private bool isLoaded = false;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        StartCoroutine(LoadTips());
    }

    private IEnumerator LoadTips()
    {
        // Wait until SupabaseManager.Instance (the Client) is ready
        yield return new WaitUntil(() => SupabaseManager.Instance != null);

        var task = SupabaseManager.Instance
            .From<TipModel>()
            .Get();

        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
        {
            Debug.LogError($"[TipDatabase] Failed to fetch tips: {task.Exception?.Message}");
            isLoaded = true;
            yield break;
        }

        allTips.Clear();
        foreach (var row in task.Result.Models)
        {
            if (!string.IsNullOrWhiteSpace(row.Tip))
                allTips.Add(row.Tip);
        }

        Debug.Log($"[TipDatabase] {allTips.Count} tips loaded from Supabase.");
        isLoaded = true;
    }

    public string GetRandomTip()
    {
        if (allTips.Count == 0)
            return string.Empty;

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
        return allTips[randomIndex];
    }

    public bool IsReady() => isLoaded;
}