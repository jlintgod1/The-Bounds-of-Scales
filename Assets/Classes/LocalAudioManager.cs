// najxbczfqxhdkofuqk@ttirv.net
// 1234567890

using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;
using UnityEngine.UI;
[System.Serializable]
public class Sound 
{
    public AudioClip clip;
    public AudioClip oggClip;
    public string name;
    [Range(0f, 1f)]
    public float volume;
    [Range(-3f, 3f)]
    public float pitch;
    public bool loop;
    [HideInInspector]
    public bool fading = false;
    [Range(0f, 1f)]
    public float spatialAmount = 0;
    public bool autoPlay = false;
    public bool hasLowPass;
    public AudioLowPassFilter lowPassFilter;
    public float lowPassCutoff = 500;
    [HideInInspector]
    public AudioSource source;
}

public class LocalAudioManager : MonoBehaviour
{
    public Sound[] sounds;
    public Toggle[] musicToggles;

    // Start is called before the first frame update
    void Awake()
    {
        //DontDestroyOnLoad(gameObject);

        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();

            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor || s.oggClip == null) {
                s.source.clip = s.clip;
            }else {
                s.source.clip = s.oggClip;
            }
            
            
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
            s.source.spatialBlend = s.spatialAmount;

            if (s.autoPlay == true) {
                Play(s.name);
            }
            if (s.hasLowPass == true) {
                s.lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
                s.lowPassFilter.cutoffFrequency = s.lowPassCutoff;
                s.lowPassFilter.enabled = false;
            }
        }
    }

    void Start()
    {
        //Play("Music");
    }

    public void crossFade(string from, string to, float time) {
        StartCoroutine(crossFadeC(from, to, time));
    }

    IEnumerator crossFadeC(string from, string to, float time) {
        float step = Time.deltaTime / time;
        Sound froms = Array.Find(sounds, sound => sound.name == from);
        Sound tos = Array.Find(sounds, sound => sound.name == to);
        
        if (froms == null) {
            Debug.LogWarning(from + " doesn't exist!");
            yield break;
        }

        if (tos == null) {
            Debug.LogWarning(to + " doesn't exist!");
            yield break;
        }

        froms.fading = true;
        tos.source.volume = 0;
        while (froms.source.volume > 0.05)
        {
            float curVel = 0;
            froms.source.volume = Mathf.SmoothDamp(froms.source.volume, 0, ref curVel, time);
            yield return null;
        }
        froms.source.volume = 0;
        froms.source.Stop();
        tos.source.Play();
        tos.fading = true;
        froms.fading = false;

        while (tos.source.volume < tos.volume-0.05f)
        {
            float curVel = 0;
            tos.source.volume = Mathf.SmoothDamp(tos.source.volume, tos.volume, ref curVel, time);
            yield return null;
        }
        tos.source.volume = tos.volume;
    }

    public void turnOff(string name) {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) {
            //Debug.LogWarning(name + " doesn't exist!");
            return;
        }
        s.source.pitch = 0;
    }

    public void turnOn(string name) {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) {
            //Debug.LogWarning(name + " doesn't exist!");
            return;
        }
        s.source.pitch = 1;
    }

    public void fadeOut(string name, float time) {
        StartCoroutine(fadeOutC(name, time));
    }


    IEnumerator fadeOutC(string name, float time) {
        float step = Time.deltaTime / time;
        Sound s = Array.Find(sounds, sound => sound.name == name);
        
        if (s == null) {
            Debug.LogWarning(name + " doesn't exist!");
            yield break;
        }

        s.fading = true;
        while (s.source.volume > 0.01f)
        {
            float curVel = 0;
            s.source.volume = Mathf.SmoothDamp(s.source.volume, 0, ref curVel, time);
            yield return null;
        }
        
        s.source.volume = 0;
        s.fading = false;
        Stop(name);
    }

    public void Stop(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) {
            //Debug.LogWarning(name + " doesn't exist!");
            return;
        }
        s.source.Stop();
    }
    public void PlayBecauseUnityIsWhack(string name)
    {
        Play(name);
    }

    public void Play(string name, float delay=0) 
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) {
            //Debug.LogWarning(name + " doesn't exist!");
            return;
        }
        StartCoroutine(PlayDelayed(s, delay));


    }

    IEnumerator PlayDelayed(Sound s, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (s.fading == false) {
            s.source.volume = s.volume;
        }
        s.source.Play();
    }

    public void Pause(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) {
            //Debug.LogWarning(name + " doesn't exist!");
            return;
        }
        s.source.Pause();
    }

    public bool GetState(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) {
            //Debug.LogWarning(name + " doesn't exist!");
            return false;
        }
        return s.source.isPlaying;
    }

    public Sound GetSound(string name)
    {
        Sound s = Array.Find(sounds, sound => sound.name == name);
        if (s == null) {
            //Debug.LogWarning(name + " doesn't exist!");
            return null;
        }
        return s;
    }
}
