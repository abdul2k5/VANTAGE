using UnityEngine;

/// <summary>
/// Central audio hub: master/music/SFX volume (sliders in Settings, persisted via PlayerPrefs),
/// one-shot SFX playback, and menu/gameplay music switching. Individual scripts keep their own
/// AudioClip fields (same pattern as the existing beepSound/hoverHum/headshotSound) and route
/// playback through here so volume control is centralized instead of duplicated per script.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music")]
    public AudioClip menuMusic;
    public AudioClip gameplayMusic;
    /// <summary>Swapped in for the last room (The Round) - everything up to that point shares gameplayMusic.</summary>
    public AudioClip roundMusic;

    [Header("UI")]
    public AudioClip uiClickSound;

    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.6f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    AudioSource musicSource;
    AudioSource sfxSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        masterVolume = PlayerPrefs.GetFloat("VantageMasterVolume", masterVolume);
        musicVolume = PlayerPrefs.GetFloat("VantageMusicVolume", musicVolume);
        sfxVolume = PlayerPrefs.GetFloat("VantageSfxVolume", sfxVolume);

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        ApplyMusicVolume();
        PlayMenuMusic();
    }

    /// <summary>Scales a positional one-shot by SFX+master volume - use for AudioSource.PlayClipAtPoint callers (e.g. an object about to be destroyed, like a killed Sentinel).</summary>
    public float ResolvedSfxVolume { get { return sfxVolume * masterVolume; } }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume * masterVolume * volumeScale);
    }

    public void PlayUIClick()
    {
        PlaySFX(uiClickSound);
    }

    public void PlayMenuMusic()
    {
        PlayMusic(menuMusic);
    }

    public void PlayRoundMusic()
    {
        PlayMusic(roundMusic);
    }

    public void PlayGameplayMusic()
    {
        PlayMusic(gameplayMusic);
    }

    void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    void ApplyMusicVolume()
    {
        if (musicSource != null) musicSource.volume = musicVolume * masterVolume;
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("VantageMasterVolume", masterVolume);
        ApplyMusicVolume();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("VantageMusicVolume", musicVolume);
        ApplyMusicVolume();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("VantageSfxVolume", sfxVolume);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
