using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : BaseMenu
{
    public static bool Paused { get; private set; }
    public GameObject Content;
    public GameObject Dialog;
    public Button DialogYesButton;
    public Slider[] VolumeSliders = new Slider[2];
    public Button QuitButton;
    private InputActions inputActions;

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            Pause(true);
    }

    public void Pause(bool state)
    {
        if (Paused == state) return;
        if (!Paused && GameManager.Instance.GameState != 1) return;

        Paused = state;
        Content.SetActive(state);
        Dialog.SetActive(state && Dialog.activeSelf);
        GameManager.Instance.SetGameState(state ? 0 : 1);
        audioMixer.TransitionToSnapshots(
            new AudioMixerSnapshot[] { audioMixer.FindSnapshot("Snapshot"), audioMixer.FindSnapshot("Paused") },
            new float[] { Paused ? 0 : 1, Paused ? 1 : 0 },
            0.05f);

        float MusicVolume, SFXVolume;
        audioMixer.GetFloat("MusicVolume", out MusicVolume);
        audioMixer.GetFloat("SFXVolume", out SFXVolume);

        VolumeSliders[0].value = CalculateAudioMixerVolume(MusicVolume, false);
        VolumeSliders[1].value = CalculateAudioMixerVolume(SFXVolume, false);
    }

    private void Awake()
    {
        inputActions = new();
        inputActions.Enable();

        if (Application.platform == RuntimePlatform.WebGLPlayer)
            QuitButton.gameObject.SetActive(false);

    }

    // Update is called once per frame
    void Update()
    {
        if (inputActions.Menu.Pause.WasPressedThisFrame())
            Pause(!Paused);
    }

    public void EndDialog(string Callback = "")
    { 
        if (string.IsNullOrEmpty(Callback))
        {
            Content.SetActive(true);
            Dialog.SetActive(false);
        }
        else
        {
            Content.SetActive(false);
            Dialog.SetActive(false);
            Paused = false;
            audioMixer.TransitionToSnapshots(
                new AudioMixerSnapshot[] { audioMixer.FindSnapshot("Snapshot"), audioMixer.FindSnapshot("Paused") },
                new float[] { 1, 0 },
                1f);
            SendMessage(Callback, false);
        }
    }

    public void InitiateDialog(string YesCallback)
    {
        Content.SetActive(false);
        Dialog.SetActive(true);
        DialogYesButton.onClick.RemoveAllListeners();
        DialogYesButton.onClick.AddListener(() => EndDialog(YesCallback));
    }

    IEnumerator MainMenuCoroutine(bool restart)
    {
        GameManager.Instance.MusicManager.FadeSound(GameManager.Instance.MusicManager.ActiveSounds[0], 0.5f, 0);
        GameManager.Instance.UI.TriggerCircleFade(false, 0.5f, Color.black, new(0.5f, 0.5f));
        foreach (var item in GameManager.Instance.UI.PanelAnimators)
        {
            item.SetBool("Active", false);
        }
        yield return new WaitForSecondsRealtime(0.75f);
        GameManager.Instance.UI.TriggerCircleFade(true, 0.5f, Color.black, new(0.5f, 0.5f));
        if (restart)
        {
            GameManager.Instance.StartGame(true);
        }
        else
        {
            GameManager.Instance.UI_MainMenu.gameObject.SetActive(true);
            GameManager.Instance.MusicManager.PlaySound(GameManager.Instance.UI_MainMenu.menuMusic);
        }
    }

    public void OnGoToMainMenu()
    {
        StartCoroutine(MainMenuCoroutine(false));
    }

    public override void OnStartGame(bool WipeSaveFile=false)
    {
        StartCoroutine(MainMenuCoroutine(true));
    }
}
