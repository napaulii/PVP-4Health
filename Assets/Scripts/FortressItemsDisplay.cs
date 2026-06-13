using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class FortressItemsDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform container;
    [SerializeField] private GameObject itemButtonPrefab;
    [SerializeField] private PlacementSystem placementSystem;
    [SerializeField] private Sprite[] fortressSprites;

    [Header("Fortress database IDs one per level, in order")]
    [SerializeField] private int[] fortressIDs = { 100, 101, 102, 103 };

    [Header("Fortress display names one per level")]
    [SerializeField] private string[] fortressNames = { "Tent", "Cabin", "House", "Castle" };

    async void OnEnable()
    {
        Debug.Log("[Fortress UI] Enabled");
        FortressUpdateScript.OnLevelReached += OnLevelReached;

        var fortressScript = FindFirstObjectByType<FortressUpdateScript>();
        if (fortressScript != null)
        {
            await fortressScript.UpdateFortressModelAsync();

            // If still -1, wait a frame and try once more
            if (FortressUpdateScript.CurrentLevel < 0)
            {
                await System.Threading.Tasks.Task.Delay(500);
                await fortressScript.UpdateFortressModelAsync();
            }

            Debug.Log($"[Fortress UI] Level after fetch: {FortressUpdateScript.CurrentLevel}");
        }

        Refresh();
    }

    void OnDisable()
    {
        FortressUpdateScript.OnLevelReached -= OnLevelReached;
    }

    void OnLevelReached(int newLevel)
    {
        // Add just the newly unlocked fortress item
        Refresh();
    }

    public void Refresh()
    {
        Debug.Log($"CurrentLevel = {FortressUpdateScript.CurrentLevel}");

        foreach (Transform child in container)
            Destroy(child.gameObject);

        int unlockedUpTo = FortressUpdateScript.CurrentLevel;

        Debug.Log($"[Fortress UI] Adding fortresses 0 > {unlockedUpTo}");

        for (int i = 0; i <= unlockedUpTo; i++)
            AddFortressButton(i);
    }

    void AddFortressButton(int level)
    {

        Debug.Log($"[Fortress UI] Creating button level {level}");
        if (level < 0 || level >= fortressIDs.Length) return;

        // Don't add duplicates if Refresh is called multiple times
        foreach (Transform child in container)
            if (child.name == $"Fortress_{level}") return;

        GameObject btn = Instantiate(itemButtonPrefab, container);
        btn.name = $"Fortress_{level}";
        btn.SetActive(true);

        var img = btn.transform.Find("Image")?.GetComponent<Image>();
        if (img != null && fortressSprites != null && level < fortressSprites.Length)
            img.sprite = fortressSprites[level];

        var label = btn.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (label != null && level < fortressNames.Length)
            label.text = fortressNames[level];

        int capturedID = fortressIDs[level];
        var button = btn.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                Debug.Log($"[Fortress] Selected level {level} ID {capturedID}");
                placementSystem.StartPlacement(capturedID);
                EventSystem.current.SetSelectedGameObject(null);
            });
        }
    }
}