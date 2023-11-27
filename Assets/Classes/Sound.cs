using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "Sound", menuName = "ScriptableObjects/Sound", order = 2)]
public class Sound : ScriptableObject
{
    public AudioClip clip;
    public AudioClip oggClip;
    [Range(0f, 1f)]
    public float volume = 1;
    [Range(-3f, 3f)]
    public float pitch = 1;
    public bool loop;
    [Range(0f, 1f)]
    public float spatialAmount = 0;
    public AudioMixerGroup audioMixerGroup;
    public bool hasLowPass;
    public float lowPassCutoff = 500;
}