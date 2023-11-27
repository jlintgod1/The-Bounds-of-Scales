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
    public Animator[] PanelAnimators;
    public Slider Timer;
    public TMP_Text LifestealEnemiesRemaining;
    public Material CircleFade;
    public bool CircleFadeState;

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

    IEnumerator CircleFadeCoroutine(bool fadeIn, float time, Color color, Vector2 position)
    {
        CircleFadeState = fadeIn;
        CircleFade.SetColor("_Color", color);
        CircleFade.SetVector("_Position", new Vector2(Mathf.Clamp01(position.x), Mathf.Clamp01(position.y)));

        float startAlpha = fadeIn ? 0 : 1.2f;
        float endAlpha = 1.2f - startAlpha;

        for (float i = 0; i <= 1; i+=Time.unscaledDeltaTime / time)
        {
            CircleFade.SetFloat("_Radius", Mathf.Lerp(startAlpha, endAlpha, i));
            yield return null;
        }
        CircleFade.SetFloat("_Radius", endAlpha);
    }

    public void TriggerCircleFade(bool fadeIn, float time, Color color, Vector2 position)
    {
        if (CircleFadeState != fadeIn)
            StartCoroutine(CircleFadeCoroutine(fadeIn, time, color, position));
    }
}
