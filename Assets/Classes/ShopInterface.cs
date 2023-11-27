using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static GameManager;

public class ShopInterface : MonoBehaviour
{
    public GameObject ShopItemTemplate;
    public GameObject ShopItemList;
    public Sound ShopMusic;
    
    List<UpgradeItem> upgradeItems = new List<UpgradeItem>();
    List<ShopItemController> ShopItems = new List<ShopItemController>();

    public bool HasPrerequisites(string[] Prerequisites)
    {
        foreach (var item in Prerequisites)
        {
            if (GameManager.Instance.GetUpgradeCount(item) <= 0) return false;
        }
        return true;
    }
    public void UpdateShop()
    {
        foreach (var item in ShopItems)
        {
            Destroy(item.gameObject);
        }
        ShopItems.Clear();

        upgradeItems.Clear();
        List<UpgradeItem> soldOutItems = new List<UpgradeItem>();
        foreach (var item in GameManager.Instance.UpgradeDatabase)
        {
            if (GameManager.Instance.GetUpgradeCount(item.ID) >= item.MaxBuyCount)
            {
                soldOutItems.Add(item);
            }
            else if (HasPrerequisites(item.Prerequisites))
            {
                upgradeItems.Add(item);
            }

        }
        upgradeItems.AddRange(soldOutItems);

        for (int i = 0; i < upgradeItems.Count; i++)
        {
            var newListEntry = Instantiate(ShopItemTemplate, ShopItemList.transform);
            newListEntry.GetComponent<RectTransform>().anchoredPosition = new(0, i * -68);
            newListEntry.GetComponent<RectTransform>().sizeDelta = new(0, 64);
            newListEntry.GetComponent<ShopItemController>().SetItemData(this, upgradeItems[i], i >= (upgradeItems.Count - soldOutItems.Count));
            ShopItems.Add(newListEntry.GetComponent<ShopItemController>());
        }
        ShopItemList.GetComponent<RectTransform>().sizeDelta = new(0, upgradeItems.Count * 68);
    }
}