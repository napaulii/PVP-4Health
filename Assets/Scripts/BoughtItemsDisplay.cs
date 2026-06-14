using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using SupabaseModels;

public class BoughtItemsDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform container;
    [SerializeField] private GameObject itemButtonPrefab;
    [SerializeField] private PlacementSystem placementSystem;
    [SerializeField] private Sprite[] itemSprites;



    private GroupController _groupController = new GroupController();
    private PersonalItemController _personalItemController = new PersonalItemController();

    private static readonly Item.ItemType[] ALL_ITEMS =
    {
        Item.ItemType.Tree1,
        Item.ItemType.Tree2,
        Item.ItemType.Tree3,
        Item.ItemType.Bush1,
        Item.ItemType.Bush2,
        Item.ItemType.Statue1,
        Item.ItemType.Statue2,
        Item.ItemType.Fountain1,
    };

    private async void OnEnable()
    {
        Debug.Log("[BoughtItems] Loading owned items...");

        List<PersonalItem> myItems =
            await _personalItemController.GetAllPersonalItemsAsync();

        Debug.Log($"[BoughtItems] My items count: {myItems?.Count ?? 0}");

        foreach (PersonalItem dbItem in myItems)
        {
            Item.ItemType? type = GetTypeFromTitle(dbItem.Title);
            if (type.HasValue)
            {
                Item.Unlock(type.Value);
                Debug.Log($"Unlocked: {type.Value}");
            }
        }

        Refresh();
    }

    private Item.ItemType? GetTypeFromTitle(string title)
    {
        switch (title)
        {
            case "Tree1":
                return Item.ItemType.Tree1;

            case "Tree2":
                return Item.ItemType.Tree2;

            case "Tree3":
                return Item.ItemType.Tree3;

            case "Green Bush":
                return Item.ItemType.Bush1;

            case "Orange Bush":
                return Item.ItemType.Bush2;

            case "Man Statue":
                return Item.ItemType.Statue1;

            case "Woman Statue":
                return Item.ItemType.Statue2;

            case "Fountain":
                return Item.ItemType.Fountain1;

            case "Halloween Theme":
                return Item.ItemType.Theme1;

            default:
                return null;
        }
    }

    public void Refresh()
    {
        if (container == null)
        {
            Debug.LogError("Container is NULL");
            return;
        }

        if (itemButtonPrefab == null)
        {
            Debug.LogError("ItemButtonPrefab is NULL");
            return;
        }

        Debug.Log($"Container = {container.name}");

        // CLEAR EXISTING ITEMS
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }

        bool anyOwned = false;

        foreach (Item.ItemType type in ALL_ITEMS)
        {
            bool owned = Item.IsOwned(type);

            Debug.Log($"{type} owned = {owned}");

            if (!owned)
                continue;

            anyOwned = true;

            GameObject btn = Instantiate(itemButtonPrefab, container);

            btn.name = type.ToString();

            // Ensure active
            btn.SetActive(true);

            // Set icon
            Image img = btn.transform.Find("Image")?.GetComponent<Image>();

            int spriteIndex = (int)type;

            if (img != null &&
                itemSprites != null &&
                spriteIndex >= 0 &&
                spriteIndex < itemSprites.Length)
            {
                img.sprite = itemSprites[spriteIndex];
            }

            // Set label
            TextMeshProUGUI label =
                btn.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();

            if (label != null)
            {
                label.text = Item.GetName(type);
            }

            // Click / drag handling
            int capturedID = (int)type;

            Button button = btn.GetComponent<Button>();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();

                button.onClick.AddListener(() =>
                {
                    Debug.Log($"Selected item ID = {capturedID}");

                    placementSystem.StartPlacement(capturedID);

                    EventSystem.current.SetSelectedGameObject(null);
                });
            }

            Debug.Log($"Created UI item: {type}");
        }

        Debug.Log($"Final child count = {container.childCount}");

        if (!anyOwned)
        {
            Debug.LogWarning("No owned items found.");
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            container.GetComponent<RectTransform>());
    }
}