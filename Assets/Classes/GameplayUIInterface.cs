using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayUIInterface : MonoBehaviour
{
    public TMP_Text VenomCounter;
    public Image[] HealthBars;
    public Sprite[] HealthBarSprites;

    public void UpdateHealth(int Health, int MaxHealth)
    {
        for (int i = 0; i < HealthBars.Length; i++)
        {
            bool isFilled = Health > i;
            HealthBars[i].color = isFilled ? Color.red : Color.black;
            HealthBars[i].sprite = HealthBarSprites[isFilled ? 1 : 0];
            HealthBars[i].gameObject.SetActive(i < MaxHealth);
        }
    }
}
