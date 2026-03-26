using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopScript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Sprite[] itemSprites;
    [SerializeField] private TextMeshProUGUI coinText;

    private Transform container;
    private Transform itemTemplate;

    private static readonly Item.ItemType[] ALL_ITEMS =
    {
        Item.ItemType.Tree1,
        Item.ItemType.Tree2,
        Item.ItemType.Tree3,
        Item.ItemType.Bush1,
        Item.ItemType.Bush2,
        Item.ItemType.Bush3,
        Item.ItemType.Statue1,
        Item.ItemType.Theme1,
    };

    private void Awake()
    {
        Item.ResetAll();

        container = transform.Find("Container");
        itemTemplate = container.Find("Item");
        itemTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (CoinManager.Instance != null)
            CoinManager.Instance.OnCoinsChanged.AddListener(OnCoinsChanged);

        UpdateCoinDisplay();

        foreach (Item.ItemType type in ALL_ITEMS)
            CreateItemButton(type);
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
            SetButtonOwned(buyButton, false);

            Item.ItemType capturedType = itemType;
            Button capturedBtn = buyButton;
            buyButton.onClick.AddListener(() => OnBuyClicked(capturedType, cost, capturedBtn));
        }
    }

    private void OnBuyClicked(Item.ItemType itemType, int cost, Button button)
    {
        if (Item.IsOwned(itemType)) return;

        if (CoinManager.Instance == null || !CoinManager.Instance.TrySpend(cost))
        {
            Debug.Log("Not enough coins!");
            return;
        }

        Item.Unlock(itemType);
        Debug.Log($"Purchased {itemType} for {cost} coins.");
        SetButtonOwned(button, true);
        UpdateCoinDisplay();
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