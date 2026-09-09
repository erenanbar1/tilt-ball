using UnityEngine;
using UnityEngine.UI;

// One button, reused for both the music and SFX toggles in the pause menu.
// Swaps its own icon to match AudioManager's state — on click and on Start, so
// it always opens showing whatever the player last set, not a default.
[RequireComponent(typeof(Image), typeof(Button))]
public class AudioToggleButton : MonoBehaviour
{
    public enum Kind { Music, Sfx }

    public Kind kind;
    public Sprite onSprite;
    public Sprite offSprite;

    Image image;

    void Awake()
    {
        image = GetComponent<Image>();
        GetComponent<Button>().onClick.AddListener(Toggle);
    }

    void Start()
    {
        Refresh();
    }

    void Toggle()
    {
        if (AudioManager.Instance == null) return;

        if (kind == Kind.Music) AudioManager.Instance.ToggleMusic();
        else AudioManager.Instance.ToggleSfx();

        // After the toggle, not before: switching SFX off means this click is
        // the last thing muted, not the first thing after — switching it back
        // on confirms audibly, which is the behaviour that reads as correct.
        AudioManager.PlayClick();
        Refresh();
    }

    void Refresh()
    {
        if (AudioManager.Instance == null || image == null) return;

        bool enabled = kind == Kind.Music ? AudioManager.Instance.MusicEnabled : AudioManager.Instance.SfxEnabled;
        image.sprite = enabled ? onSprite : offSprite;
    }
}
