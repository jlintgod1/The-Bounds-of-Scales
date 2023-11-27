using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class MainMenu : BaseMenu
{
    public Sound menuMusic;
    ActiveSound musicInstance;
    public List<Slider> VolumeSliders;

    public List<GameObject> SaveFileContent;
    public TMP_Text SnakeVenomLabel;
    public TMP_Text UpgradesLabel;
    public TMP_Text TimeLabel;
    public Slider ProgressBar;

    // Start is called before the first frame update
    void Start()
    {
        musicInstance = GameManager.Instance.MusicManager.PlaySound(menuMusic);

        VolumeSliders[0].value = PlayerPrefs.GetFloat("MusicVolume", 1);
        OnChangeMusicVolume(VolumeSliders[0].value);
        VolumeSliders[1].value = PlayerPrefs.GetFloat("SFXVolume", 1);
        OnChangeSFXVolume(VolumeSliders[1].value);

        SaveFileContent[0].SetActive(string.IsNullOrEmpty(SaveManager.saveData.dataChecksum));
        SaveFileContent[1].SetActive(!SaveFileContent[0].activeSelf);
        SnakeVenomLabel.text = "x" + SaveManager.saveData.SnakeVenomCount;
        UpgradesLabel.text = SaveManager.saveData.OwnedUpgrades.Count + "/" + GameManager.Instance.UpgradeDatabase.Count;
        TimeLabel.text = Mathf.FloorToInt(SaveManager.saveData.TimeSpent / 3600) + 
            (SaveManager.saveData.TimeSpent > 359940 ? " hrs" : (":" + Mathf.FloorToInt(SaveManager.saveData.TimeSpent / 60)));
        ProgressBar.value = 48 * (SaveManager.saveData.CompletedTutorials.Count / (GameManager.Instance.AllLevelThemes.Count + 2));

        GameManager.Instance.UI.CircleFade.SetFloat("_Radius", 0);
        StartCoroutine(PostStart());
    }

    IEnumerator PostStart()
    {
        yield return null;
        GameManager.Instance.UI.TriggerCircleFade(true, 1, Color.black, new(0.5f, 0.5f));
    }

    private void Update()
    {
        GetComponent<Image>().material.SetFloat("_RealTime", Time.unscaledTime);
    }


}
