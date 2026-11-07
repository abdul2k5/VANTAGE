using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Central audio hub: owns the VantageMixer (Master/Music/SFX groups), one-shot SFX playback,
/// menu/gameplay music switching, and a safety-net sweep that catches any AudioSource created
/// elsewhere in the project without an output group assigned (so nothing can silently bypass
/// the mixer). Individual scripts keep their own AudioClip fields and either call PlaySFX /
/// PlaySfxAtPoint here, or create their own AudioSource and route it via SfxGroup/MusicGroup.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Mixer")]
    public AudioMixer mixer;
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;

    [Header("Music")]
    public AudioClip menuMusic;
    public AudioClip gameplayMusic;
    /// <summary>Swapped in for the last room (The Round) - everything up to that point shares gameplayMusic.</summary>
    public AudioClip roundMusic;

    [Header("UI")]
    public AudioClip uiClickSound;

    [Range(0f, 1f)] public float masterVolume = 0.8f;
    [Range(0f, 1f)] public float musicVolume = 0.8f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    const string MasterParam = "MasterVolume";
    const string MusicParam = "MusicVolume";
    const string SfxParam = "SFXVolume";
    const float MinLinearVolume = 0.0001f;

    AudioSource musicSource;
    AudioSource sfxSource;

    public AudioMixerGroup MusicGroup { get { return musicGroup; } }
    public AudioMixerGroup SfxGroup { get { return sfxGroup; } }

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
        musicSource.outputAudioMixerGroup = musicGroup;
        // Menu music must keep playing while AudioListener.pause is set for the main menu/settings.
        musicSource.ignoreListenerPause = true;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.outputAudioMixerGroup = sfxGroup;

        PlayMenuMusic();

        StartCoroutine(RouteUnassignedAudioSources());
    }

    void Start()
    {
        // AudioMixer.SetFloat is ignored during Awake, so the saved volumes only take effect from here.
        ApplyMasterMixerVolume();
        ApplyMusicMixerVolume();
        ApplySfxMixerVolume();
    }

    /// <summary>Converts a 0-1 slider value to the dB a mixer exposed-parameter expects (log curve).</summary>
    public static float LinearToDecibel(float value)
    {
        return Mathf.Log10(Mathf.Max(value, MinLinearVolume)) * 20f;
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volumeScale);
    }

    /// <summary>Positional one-shot routed through the SFX mixer group - use instead of AudioSource.PlayClipAtPoint, which has no way to target a mixer group.</summary>
    public void PlaySfxAtPoint(AudioClip clip, Vector3 position, float volumeScale = 1f)
    {
        if (clip == null) return;
        GameObject go = new GameObject("OneShotSFX_" + clip.name);
        go.transform.position = position;
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volumeScale;
        src.spatialBlend = 1f;
        src.minDistance = 2f;
        src.maxDistance = 20f;
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.outputAudioMixerGroup = sfxGroup;
        src.Play();
        Destroy(go, clip.length + 0.1f);
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

    void ApplyMasterMixerVolume()
    {
        if (mixer != null) mixer.SetFloat(MasterParam, LinearToDecibel(masterVolume));
    }

    void ApplyMusicMixerVolume()
    {
        if (mixer != null) mixer.SetFloat(MusicParam, LinearToDecibel(musicVolume));
    }

    void ApplySfxMixerVolume()
    {
        if (mixer != null) mixer.SetFloat(SfxParam, LinearToDecibel(sfxVolume));
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("VantageMasterVolume", masterVolume);
        ApplyMasterMixerVolume();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("VantageMusicVolume", musicVolume);
        ApplyMusicMixerVolume();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("VantageSfxVolume", sfxVolume);
        ApplySfxMixerVolume();
    }

    /// <summary>
    /// Safety net: any AudioSource created anywhere in the project (now or in a future script)
    /// that forgets to set outputAudioMixerGroup would otherwise bypass the mixer entirely and
    /// ignore the volume sliders. Sweeps periodically and defaults stragglers to SFX.
    /// </summary>
    IEnumerator RouteUnassignedAudioSources()
    {
        WaitForSeconds wait = new WaitForSeconds(0.5f);
        yield return wait; // let every script's own Start() route its AudioSource first - this is a backstop, not the primary path
        while (true)
        {
            AudioSource[] all = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                AudioSource src = all[i];
                if (src == null || src == musicSource) continue;
                if (src.outputAudioMixerGroup == null)
                {
                    src.outputAudioMixerGroup = sfxGroup;
                    Debug.LogWarning("AudioManager: routed unassigned AudioSource on '" + src.gameObject.name + "' to SFX group.", src);
                }
            }
            yield return wait;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
