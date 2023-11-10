using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelExit : MonoBehaviour
{
    public TMP_Text LevelText;
    public LevelTheme LevelTheme;
    public int ExitIndex;
    
    public void UpdateLevelExit(LevelTheme level)
    {
        LevelTheme = level;
        LevelText.text = level.LevelName;
    }

    // Update is called once per frame
    void Update()
    {
        float distance = (transform.position.y - GameManager.Instance.Player.transform.position.y) / 2f;
        //LevelText.rectTransform.anchoredPosition = new Vector2(0, (1 - Mathf.Clamp01(Mathf.Abs(distance))) / 2f * Mathf.Sign(distance));
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            GameManager.Instance.OnEnterLevelExit(LevelTheme);
        }
    }
}
