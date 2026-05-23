using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using SupabaseModels;
using static Item;

public class ShopScript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Sprite[] itemSprites;
    [SerializeField] private TextMeshProUGUI coinText;

    [Header("References")]
    [SerializeField] private GridSystem gridSystem;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject HomePanel;
    [SerializeField] private GameObject HomeBottomPanel;

    private Transform container;
    private Transform itemTemplate;

    private PersonalItemController _personalItemController;
    private GroupController _groupController;

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
        Item.ItemType.Theme1,
    };

    public static string GetName(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Tree1: return "Green Tree";
            case ItemType.Tree2: return "Orange Tree";
            case ItemType.Tree3: return "Pink Tree";
            case ItemType.Bush1: return "Green Bush";
            case ItemType.Bush2: return "Orange Bush";
            case ItemType.Statue1: return "Man Statue";
            case ItemType.Statue2: return "Warrior Statue";
            case ItemType.Fountain1: return "Fountain";
            case ItemType.Theme1: return "Halloween Theme";
            default: return itemType.ToString();
        }
    }

    private void Awake()
    {
        Item.ResetAll();

        container = transform.Find("Scroll View/Viewport/Container");
        itemTemplate = container.Find("Item");
        itemTemplate.gameObject.SetActive(false);

        _personalItemController = new PersonalItemController();
        _groupController = new GroupController();
    }

    private async void Start()
    {
        if (CoinManager.Instance != null)
            CoinManager.Instance.OnCoinsChanged.AddListener(OnCoinsChanged);

        UpdateCoinDisplay();

        // Load all items purchased by anyone in the group
        await LoadGroupOwnedItems();

        foreach (Item.ItemType type in ALL_ITEMS)
            CreateItemButton(type);
    }

    private async System.Threading.Tasks.Task LoadGroupOwnedItems()
    {
        List<PersonalItem> groupItems = await _groupController.GetGroupPurchasedItemsAsync();

        foreach (PersonalItem dbItem in groupItems)
        {
            if (System.Enum.TryParse(dbItem.Title, out Item.ItemType type))
            {
                Item.Unlock(type);
                Debug.Log($"[Shop] Unlocked from group purchase: {type} (owner: {dbItem.UserId})");
            }
        }
    }

    private void CreateItemButton(Item.ItemType itemType)
    {
        Transform clone = Instantiate(itemTemplate, container);
        clone.gameObject.SetActive(true);

        TextMeshProUGUI nameLabel = clone.Find("Name")?.GetComponent<TextMeshProUGUI>();
        if (nameLabel != null) nameLabel.SetText(Item.GetName(itemType));

        int cost = Item.GetCost(itemType);
        TextMeshProUGUI priceLabel = clone.Find("Price")?.GetComponent<TextMeshProUGUI>();
        if (priceLabel != null) priceLabel.SetText(cost.ToString());

        Image itemImage = clone.Find("Image")?.GetComponent<Image>();
        if (itemImage != null)
        {
            int index = (int)itemType;
            if (itemSprites != null && index < itemSprites.Length && itemSprites[index] != null)
                itemImage.sprite = itemSprites[index];
        }

        Button buyButton = clone.Find("BuyButton")?.GetComponent<Button>();
        if (buyButton != null)
        {
            SetButtonOwned(buyButton, Item.IsOwned(itemType));

            Item.ItemType capturedType = itemType;
            Button capturedBtn = buyButton;
            buyButton.onClick.AddListener(() => OnBuyClicked(capturedType, cost, capturedBtn));
        }
    }

    private async void OnBuyClicked(Item.ItemType itemType, int cost, Button button)
    {
        if (Item.IsOwned(itemType)) return;

        if (CoinManager.Instance == null || !(await CoinManager.Instance.TrySpend(cost)))
        {
            Debug.Log("[Shop] Not enough coins (or DB error)!");
            return;
        }

        // Save purchase to Supabase
        PersonalItem saved = await _personalItemController.CreatePersonalItemAsync(
            title: Item.GetName(itemType),
            description: $"Purchased {Item.GetName(itemType)}",
            price: cost
        );

        if (saved == null)
        {
            Debug.LogWarning($"[Shop] Failed to save purchase of {itemType} to DB.");
            return;
        }

        Item.Unlock(itemType);
        Debug.Log($"[Shop] Purchased {itemType} for {cost} coins.");
        SetButtonOwned(button, true);
        UpdateCoinDisplay();

        // Validate gridSystem before switching panels
        if (gridSystem == null)
        {
            Debug.LogError("[Shop] GridSystem reference is null! Assign it in the Inspector.");
            return;
        }

        // Only switch panels if placement started successfully
        bool placementStarted = gridSystem.StartPlacement(itemType);
        if (placementStarted)
        {
            shopPanel.SetActive(false);
            HomePanel.SetActive(true);
            HomeBottomPanel.SetActive(true);
        }
        else
        {
            Debug.LogError($"[Shop] Placement failed for {itemType}. Check GridSystem placeableItems in the Inspector.");
        }
    }

    private void SetButtonOwned(Button button, bool owned)
    {
        button.interactable = !owned;
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) label.SetText(owned ? "Owned" : "Buy");
    }

    private void OnCoinsChanged(int newAmount)
    {
        UpdateCoinDisplay();
        RefreshAllButtonStates();
    }

    private void UpdateCoinDisplay()
    {
        if (coinText != null && CoinManager.Instance != null)
            coinText.SetText(CoinManager.Instance.Coins.ToString());
    }

    private void RefreshAllButtonStates()
    {
        int currentCoins = CoinManager.Instance != null ? CoinManager.Instance.Coins : 0;

        for (int i = 1; i < container.childCount; i++)
        {
            Transform child = container.GetChild(i);
            if (!child.gameObject.activeSelf) continue;

            Item.ItemType type = ALL_ITEMS[i - 1];
            Button btn = child.Find("BuyButton")?.GetComponent<Button>();
            if (btn == null) continue;

            if (!Item.IsOwned(type))
                btn.interactable = currentCoins >= Item.GetCost(type);
        }
    }
}