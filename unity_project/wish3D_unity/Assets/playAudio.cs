using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PlayAudio : MonoBehaviour
{
    public AudioSource audioSource;

    public void PlayClip(AudioClip clip, float volume)
    {
        audioSource.PlayOneShot(clip, volume);
    }
}