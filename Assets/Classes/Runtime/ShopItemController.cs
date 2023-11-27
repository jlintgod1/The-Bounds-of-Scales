using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameManager;
using TMPro;
using UnityEngine.UI;

public class ShopItemController : MonoBehaviour
{
    private ShopInterface ownerShop;
    string ID;
    public TMP_Text NameLabel;
    public TMP_Text DescriptionLabel;
    public Image IconElement;
    public Button BuyButton;
    public TMP_Text BuyButtonLabel;
    public Transform SoldOutOverlay;

    public void SetItemData(ShopInterface newOwner, UpgradeItem upgradeItem, bool isSoldOut)
    {
        ownerShop = newOwner;
        ID = upgradeItem.ID;
        NameLabel.text = upgradeItem.Name;
        DescriptionLabel.text = upgradeItem.Description;
        IconElement.sprite = upgradeItem.Icon;
        BuyButtonLabel.text = "x" + Mathf.FloorToInt(upgradeItem.Cost * Mathf.Pow(upgradeItem.CostMultiplier, GameManager.Instance.GetUpgradeCount(upgradeItem.ID))).ToString();

        if (isSoldOut)
        {
            BuyButton.enabled = false;
            SoldOutOverlay.gameObject.SetActive(true);
        }
    }

    public void OnBuyPressed()
    {
        bool BuyResult = GameManager.Instance.BuyUpgrade(ID);

        if (BuyResult)
        {
            ownerShop.UpdateShop();
        }
        else
        {
            CancelInvoke("PostBuyFail");
            BuyButtonLabel.color = Color.red;
            Invoke("PostBuyFail", 2);
        }

    }

    private void PostBuyFail()
    {
        BuyButtonLabel.color = Color.black;
    }
}