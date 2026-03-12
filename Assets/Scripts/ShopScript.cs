using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopScript : MonoBehaviour
{

    private Transform container;
    private Transform item;

    private void Awake()
    {
        container = transform.Find("Container");
        item = container.Find("Item");
        //item.gameObject.SetActive(false);
    }

    private void Start()
    {
        CreateItemButton("Item1", Item.GetCost(Item.ItemType.Name1), 0);
        CreateItemButton("Item2", Item.GetCost(Item.ItemType.Name2), 1);
    }

    private void CreateItemButton(/*Sprite itemSprite, */string itemName, int itemCost, int positionIndex)
    {
        Transform itemTransform = Instantiate(item, container);
        RectTransform itemRect = itemTransform.GetComponent<RectTransform>();

        float itemHeight = 180f;
        itemRect.anchoredPosition = new Vector2(0, -itemHeight * positionIndex);

        //itemTransform.Find("Name").GetComponent<TextMeshProUGUI>().SetText(itemName);
        //itemTransform.Find("Price").GetComponent<TextMeshProUGUI>().SetText(itemCost.ToString());

        Transform nameText = itemTransform.Find("itemName");
        Transform costText = itemTransform.Find("costText");

        if (nameText != null)
            nameText.GetComponent<TextMeshProUGUI>().SetText(itemName);

        if (costText != null)
            costText.GetComponent<TextMeshProUGUI>().SetText(itemCost.ToString());

        //itemTransform.Find("costText").GetComponent<Image>().sprite = itemSprite;
    }
}
