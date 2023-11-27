using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;

public class BaseMenu : MonoBehaviour
{
    public AudioMixer audioMixer;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    IEnumerator StartGameCoroutine()
    {
        GameManager.Instance.MusicManager.FadeSound(GameManager.Instance.MusicManager.ActiveSounds[0], 0.5f, 0);
        GameManager.Instance.UI.TriggerCircleFade(false, 0.5f, Color.black, new(0.5f, 0.5f));
        yield return new WaitForSecondsRealtime(0.75f);
        GameManager.Instance.UI.TriggerCircleFade(true, 0.5f, Color.black, new(0.5f, 0.5f));
        GameManager.Instance.StartGame();
    }

    public virtual void OnStartGame(bool WipeSaveFile=false)
    {
        if (WipeSaveFile)
            SaveManager.DeleteSaveFile();
        StartCoroutine(StartGameCoroutine());
    }

    IEnumerator ExitGameCoroutine()
    {
        GameManager.Instance.MusicManager.FadeSound(GameManager.Instance.MusicManager.ActiveSounds[0], 0.5f, 0);
        GameManager.Instance.UI.TriggerCircleFade(false, 0.5f, Color.black, new(0.5f, 0.5f));
        yield return new WaitForSecondsRealtime(0.75f);
        Application.Quit();
    }

    public void OnExitGame()
    {
        StartCoroutine(ExitGameCoroutine());
    }

    public void OnChangeMusicVolume(float value)
    {
        audioMixer.SetFloat("MusicVolume", CalculateAudioMixerVolume(value, true));
    }
    public void OnChangeSFXVolume(float value)
    {
        audioMixer.SetFloat("SFXVolume", CalculateAudioMixerVolume(value, true));
    }

    public static float CalculateAudioMixerVolume(float value, bool to)
    {
        if (to)
            return Mathf.Log(Mathf.Max(value, 0.0001f), 10) * 20;
        else
            return Mathf.Pow(10, value / 20);
    }
}
