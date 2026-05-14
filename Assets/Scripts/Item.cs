using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Item
{
    public enum ItemType
    {
        Tree1,
        Tree2,
        Tree3,
        Bush1,
        Bush2,
        Bush3,
        Statue1,
        Theme1
       
    }

    public static int GetCost(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Tree1: return 300;
            case ItemType.Tree2: return 300;
            case ItemType.Tree3: return 300;
            case ItemType.Bush1: return 200;
            case ItemType.Bush2: return 200;
            case ItemType.Bush3: return 200;
            case ItemType.Statue1: return 500;
            case ItemType.Theme1: return 1000;
            default: return 100;
        }
    }

    public static string GetName(ItemType itemType)
    {
        return itemType.ToString();
    }



    /*private static string PrefKey(ItemType t) => "item_owned_" + (int)t;

    public static bool IsOwned(ItemType itemType)
    {
        return PlayerPrefs.GetInt(PrefKey(itemType), 0) == 1;
    }

    public static void Unlock(ItemType itemType)
    {
        PlayerPrefs.SetInt(PrefKey(itemType), 1);
        PlayerPrefs.Save();
    }*/

    private static readonly HashSet<ItemType> ownedItems = new HashSet<ItemType>();

    public static bool IsOwned(ItemType itemType)
    {
        return ownedItems.Contains(itemType);
    }

    public static void Unlock(ItemType itemType)
    {
        ownedItems.Add(itemType);
    }

    // Called on game start to wipe any leftover state
    public static void ResetAll()
    {
        ownedItems.Clear();
    }
}