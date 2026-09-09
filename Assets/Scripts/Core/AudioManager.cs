using PrimeTween;
using UnityEngine;

// Lives in Bootstrap, persists for the whole session so music doesn't restart
// or cut out across scene swaps. Two sources: one looping for music, one
// one-shot for SFX so a sound effect never interrupts the music track.
//
// The theme ducks for the whole of a win/lose outcome: down when the ball drops
// into a hole, still down under the result screen's own sound, and back up when
// the run returns to Playing — which is what Retry and Next Level both do. The
// way back up is a state subscription rather than a call from those buttons, so
// no future exit from a result screen can forget to undo the duck.
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("Clips")]
    public AudioClip mainTheme;
    public AudioClip winHole;    // the ball dropping into the winning hole
    public AudioClip winScreen;  // the win screen opening
    public AudioClip loseHole;   // the ball dropping into a losing hole
    public AudioClip loseScreen; // the "you have failed" screen opening
    public AudioClip click;      // menu buttons — not the tilt controls, which are held rather than clicked

    [Header("Music levels")]
    [Range(0f, 1f)]
    public float musicVolume = 1f;
    // Where the theme sits while a win/lose outcome plays out, so those sounds
    // land on top of it instead of fighting it.
    [Range(0f, 1f)]
    public float duckedVolume = 0.1f;
    public float duckDuration = 0.2f;
    public float restoreDuration = 0.35f;

    Tween musicFade;

    const string MusicEnabledKey = "MusicEnabled";
    const string SfxEnabledKey = "SfxEnabled";

    // Muted independently of volume — via AudioSource.mute, not by zeroing
    // volume — so it can't collide with the duck/restore tweens above, which
    // already own that field.
    public bool MusicEnabled { get; private set; } = true;
    public bool SfxEnabled { get; private set; } = true;

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

        MusicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
        SfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;
        musicSource.mute = !MusicEnabled;
        sfxSource.mute = !SfxEnabled;
    }

    // Start, not Awake, so GameManager.Instance is set no matter which order the
    // two woke up in — same reasoning as SceneLoader's subscription.
    void Start()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += HandleStateChanged;

        musicSource.volume = musicVolume;
        PlayMusic(mainTheme);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    void HandleStateChanged(GameState state)
    {
        // Ducking starts earlier than this, with the hole sound itself — by the
        // time a win/lose state lands the ball has already fallen in. All that's
        // left here is bringing the theme back once the player moves on, which
        // Retry and Next Level both do before loading the next scene.
        if (state == GameState.Playing) RestoreMusic();
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

    // The four outcome sounds are addressed by name rather than by clip: the
    // triggers and result screens that fire them live in scenes of their own, so
    // they can't hold an inspector reference to a clip wired up on Bootstrap.
    //
    // The two hole sounds open the outcome, so they take the theme down with
    // them; the screen sounds land while it's already down.
    public void PlayWinHole() => PlayDucked(winHole);
    public void PlayLoseHole() => PlayDucked(loseHole);
    public void PlayWinScreen() => PlaySFX(winScreen);
    public void PlayLoseScreen() => PlaySFX(loseScreen);

    // Static, unlike the four above: every menu button in the game calls this, so
    // the "is there an AudioManager yet" check belongs here rather than repeated
    // at a dozen call sites. Buttons that are held rather than clicked — the tilt
    // controls — deliberately don't call it.
    public static void PlayClick()
    {
        if (Instance != null) Instance.PlaySFX(Instance.click);
    }

    public void ToggleMusic() => SetMusicEnabled(!MusicEnabled);
    public void ToggleSfx() => SetSfxEnabled(!SfxEnabled);

    public void SetMusicEnabled(bool enabled)
    {
        MusicEnabled = enabled;
        if (musicSource != null) musicSource.mute = !enabled;
        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetSfxEnabled(bool enabled)
    {
        SfxEnabled = enabled;
        if (sfxSource != null) sfxSource.mute = !enabled;
        PlayerPrefs.SetInt(SfxEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    void PlayDucked(AudioClip clip)
    {
        PlaySFX(clip);
        FadeMusicTo(duckedVolume, duckDuration);
    }

    public void RestoreMusic() => FadeMusicTo(musicVolume, restoreDuration);

    void FadeMusicTo(float volume, float duration)
    {
        if (musicSource == null) return;

        musicFade.Stop();
        // Unscaled: the pause menu freezes time, and a fade that stalls
        // half-way down would leave the theme stuck at the wrong level.
        musicFade = Tween.AudioVolume(musicSource, volume, duration, Ease.Linear, useUnscaledTime: true);
    }
}
