using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
    LocalAudioManager audioManager;
    public string currentMusic;
    public string nextMusic;
    bool fading = false;
    public void FadeOut(float time)
    {
        audioManager.fadeOut(currentMusic, time);
    }
    // Start is called before the first frame update
    void Start()
    {
        audioManager = GetComponent<LocalAudioManager>();
        audioManager.Play(currentMusic);
    }

    // Update is called once per frame
    void Update()
    {
        if (currentMusic != nextMusic && fading == false) {
            fading = true;
            audioManager.crossFade(currentMusic, nextMusic, 0.125f);
        }else if (audioManager.GetSound(currentMusic).fading == false && fading == true) {
            currentMusic = nextMusic;
        }else if (nextMusic == currentMusic && fading == true && audioManager.GetSound(currentMusic).fading == false) {
            fading = false;
        }else if (currentMusic != nextMusic && fading == true) {
            fading = false;
        }
    }
}
