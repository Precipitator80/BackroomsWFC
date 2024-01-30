using UnityEngine;

/// <summary>
/// Can be attached to an audio source to start playing its audio with a random offset.
/// Meant for audio sources that loop.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class DelayAudio : MonoBehaviour
{
    private AudioSource audioSource;

    void Start()
    {
        // Get the audio source, generate a random offset in the clip's length, apply it and play the audio source.
        audioSource = GetComponent<AudioSource>();
        float randomOffset = Random.Range(0f, audioSource.clip.length);
        audioSource.time = randomOffset;
        audioSource.Play();
    }
}