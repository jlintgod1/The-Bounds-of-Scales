// najxbczfqxhdkofuqk@ttirv.net
// 1234567890

using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;
using UnityEngine.UI;
using static Unity.VisualScripting.Member;
using Unity.VisualScripting;

public class ActiveSound : UnityEngine.Object
{
    public Sound sound;
    public AudioSource audioSource;
    public bool fading;

    public ActiveSound(Sound _sound, AudioSource _audioSource)
    {
        sound = _sound;
        audioSource = _audioSource;
        fading = false;

        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor || sound.oggClip == null)
            audioSource.clip = sound.clip;
        else
            audioSource.clip = sound.oggClip;

        audioSource.volume = sound.volume;
        audioSource.pitch = sound.pitch;
        audioSource.loop = sound.loop;
        audioSource.spatialBlend = sound.spatialAmount;
        audioSource.outputAudioMixerGroup = sound.audioMixerGroup;
    }
}

public class LocalAudioManager : MonoBehaviour
{
    public List<ActiveSound> ActiveSounds = new List<ActiveSound>();

    public ActiveSound PlaySound(Sound Sound, float Delay = 0)
    {
        ActiveSound newSound = new ActiveSound(Sound, gameObject.AddComponent<AudioSource>());

        newSound.audioSource.PlayDelayed(Delay);
        ActiveSounds.Add(newSound);
        return newSound;
        /*
        if (s.hasLowPass == true) {
            s.lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
            s.lowPassFilter.cutoffFrequency = s.lowPassCutoff;
            s.lowPassFilter.enabled = false;
        }
        */
    }

    public bool StopSound(ActiveSound activeSound)
    {
        activeSound.audioSource.Stop();
        Destroy(activeSound.audioSource);
        ActiveSounds.Remove(activeSound);
        Destroy(activeSound);
        return true;
    }

    public void PauseSound(ActiveSound activeSound, bool paused)
    {
        if (paused)
            activeSound.audioSource.Pause();
        else
            activeSound.audioSource.UnPause();
    }

    
    public void CrossFadeSound(ActiveSound From, Sound To, float time) 
    {
        StartCoroutine(CrossFadeSoundCoroutine(From, To, time));
    }

    IEnumerator CrossFadeSoundCoroutine(ActiveSound From, Sound To, float time) 
    {
        StartCoroutine(FadeSoundCoroutine(From, time, 0));
        yield return new WaitForSecondsRealtime(time);

        ActiveSound toSound = PlaySound(To);
        toSound.audioSource.volume = 0;
        yield return null;

        StartCoroutine(FadeSoundCoroutine(toSound, time, 1));
        yield return new WaitForSecondsRealtime(time);
    }
    

    public void FadeSound(ActiveSound activeSound, float time, float EndAlpha) 
    {
        StartCoroutine(FadeSoundCoroutine(activeSound, time, EndAlpha));
    }

    IEnumerator FadeSoundCoroutine(ActiveSound activeSound, float time, float EndAlpha) 
    {
        float step = Time.unscaledDeltaTime / time;
        if (activeSound.fading)
        {
            //Debug.LogWarning(name + " doesn't exist!");
            yield break;
        }

        activeSound.fading = true;
        float oldVolume = activeSound.audioSource.volume;
        for (float i = 0; i <= 1; i += step)
        {
            activeSound.audioSource.volume = Mathf.Lerp(oldVolume, activeSound.sound.volume * EndAlpha, Mathf.Pow(i, 1.5f));
            yield return null;
        }
        
        activeSound.audioSource.volume = activeSound.sound.volume * EndAlpha;
        activeSound.fading = false;
        if (activeSound.audioSource.volume <= 0)
            StopSound(activeSound);
    }
    
}
