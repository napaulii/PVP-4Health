using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Item
{
    public Sprite itemSprite;
    public enum ItemType
    {
        Name1,
        Name2,
        Name3
    }

    public static int GetCost(ItemType itemType)
    {
        switch (itemType)
        {
            default:
            case ItemType.Name1:    return 100;
            case ItemType.Name2:    return 200;
            case ItemType.Name3:    return 300;


        }
    }

    public Sprite GetSprite(ItemType itemType)
    {
        switch (itemType)
        {
            default:
            case ItemType.Name1:    return itemSprite;
            case ItemType.Name2:    return itemSprite;
            case ItemType.Name3:    return itemSprite;
        }
    }


}
