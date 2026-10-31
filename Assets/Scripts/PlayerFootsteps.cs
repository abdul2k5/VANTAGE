using UnityEngine;

/// <summary>
/// Running is loud: every running footstep plays a step sound and is heard by un-alerted Sentinels within
/// hearingRadius, which stop and turn toward it (Sentinel.HearNoise). Walking (Left Alt) and crouching are
/// silent. Gives the player a real choice on approach - fast and noisy, or slow and unheard.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerFootsteps : MonoBehaviour
{
    public AudioClip[] stepSounds;
    [Range(0f, 1f)] public float stepVolume = 0.5f;
    [Tooltip("Metres travelled per footstep while running.")]
    public float strideLength = 1.9f;
    [Tooltip("Below this horizontal speed the player is standing (or just drifting to a stop) - no steps.")]
    public float minSpeed = 1f;
    public float hearingRadius = 9f;

    PlayerController controller;
    AudioSource audioSource;
    float distanceSinceStep;
    int lastClip = -1;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        if (AudioManager.Instance != null) audioSource.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
    }

    /// <summary>True when the current movement makes noise - running on the ground.</summary>
    public bool IsLoud
    {
        get
        {
            return !controller.IsAirborne && !controller.IsCrouching && !controller.IsWalking
                && !controller.SuppressMovement && controller.HorizontalSpeed >= minSpeed;
        }
    }

    void Update()
    {
        if (MainMenuController.IsOpen) return;

        if (!IsLoud)
        {
            // Half a stride banked, so the first step after starting to run lands quickly but not instantly.
            distanceSinceStep = strideLength * 0.5f;
            return;
        }

        distanceSinceStep += controller.HorizontalSpeed * Time.deltaTime;
        if (distanceSinceStep >= strideLength)
        {
            distanceSinceStep -= strideLength;
            Step();
        }
    }

    public void Step()
    {
        if (stepSounds != null && stepSounds.Length > 0)
        {
            // Never the same clip twice in a row - repeated identical steps sound mechanical.
            int i = Random.Range(0, stepSounds.Length);
            if (stepSounds.Length > 1 && i == lastClip) i = (i + 1) % stepSounds.Length;
            lastClip = i;
            if (stepSounds[i] != null) audioSource.PlayOneShot(stepSounds[i], stepVolume);
        }

        // Current room only - a drone on the far side of a wall in the next room shouldn't react.
        int room = RoomManager.Instance != null ? RoomManager.Instance.CurrentRoom : -1;
        var sentinels = Object.FindObjectsByType<Sentinel>(FindObjectsSortMode.None);
        float r2 = hearingRadius * hearingRadius;
        for (int i = 0; i < sentinels.Length; i++)
        {
            if (room >= 0 && sentinels[i].OwnerRoomIndex != room) continue;
            if ((sentinels[i].transform.position - transform.position).sqrMagnitude <= r2) sentinels[i].HearNoise(transform.position);
        }
    }
}
