using UnityEngine;

// Lives in Bootstrap, persists for the whole session so music doesn't restart
// or cut out across scene swaps. Two sources: one looping for music, one
// one-shot for SFX so a sound effect never interrupts the music track.
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioSource musicSource;
    public AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null || musicSource == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip);
    }
}
