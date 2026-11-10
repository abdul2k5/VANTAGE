using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

/// <summary>
/// Runs the post-plant phase: a countdown ticks to detonation while reinforcement drones drop in and go for
/// the charge. Any drone that reaches it starts disarming it - kill it (or flash it) before it finishes, or
/// the round is lost. Planting also hands back one charge of each ability, so holding the site is played
/// with utility, the same way every room before it was. Dying during an active round is a loss.
/// </summary>
public class RoundController : MonoBehaviour
{
    public static RoundController Instance;

    [Header("Timer")]
    public float detonationSeconds = 20f;
    public Text timerLabel;
    /// <summary>Backing panel (parent of timerLabel) - toggle this, not timerLabel.gameObject, so the panel never shows empty.</summary>
    public GameObject timerPanel;
    public GameObject chargeIcon;
    public Color timerNormalColor = new Color(0.9f, 0.3f, 0.2f, 1f);
    public Color plantedFlashColor = Color.white;
    public BreachChargeDevice device;

    [Header("Detonation feedback")]
    public Image detonationFlash;
    public Transform cameraShakeTransform;
    public ParticleSystem detonationParticles;
    public float shakeDuration = 0.4f;
    public float shakeMagnitude = 0.18f;

    [Header("Defenders (push toward the site in waves once planted)")]
    public Sentinel[] siteDefenders;
    public float[] waveDelaysSeconds = { 0f, 10f, 20f };
    [Tooltip("Drones guarding the way in (blind corner, lane). They hold their posts after the plant - only the blast removes them.")]
    public Sentinel[] approachDrones;

    [System.Serializable]
    public class ReinforcementSpawn
    {
        public Vector3 position;
        [Tooltip("Seconds after the plant that the drone lands. The warning light starts before this.")]
        public float landTime;
    }

    [Header("Reinforcements (drop in after the plant and go for the charge)")]
    [Tooltip("Prefab asset, not a scene object - so checkpoint resets (which re-activate every scene Sentinel) never wake it early.")]
    public Sentinel reinforcementPrefab;
    public ReinforcementSpawn[] reinforcementSpawns;
    public float telegraphSeconds = 1.5f;
    public float dropHeight = 4f;
    public float dropSeconds = 0.8f;
    public Color telegraphColor = new Color(1f, 0.15f, 0.1f, 1f);
    public AudioClip telegraphSound;
    public AudioClip dropSound;

    [Header("Disarm - a drone at the charge defuses it")]
    public float disarmSeconds = 4f;
    public float disarmRadius = 1.8f;
    [Tooltip("Progress lost per second while no drone is on the charge, as a fraction of a full disarm.")]
    public float disarmDecayPerSecond = 0.25f;
    public Color disarmColor = new Color(1f, 0.8f, 0.2f, 1f);
    public AudioClip disarmTickSound;
    public AudioClip disarmedSound;
    public string disarmedMessage = "A drone disarmed the Breach Charge. Kill or flash anything that reaches it.";

    [Header("Post-plant")]
    public int plantUtilityRefill = 1;
    public string postPlantObjective = "Reinforcements inbound - keep them off the charge until it blows. (+1 of each ability)";

    [Header("Audio - accelerating beep as detonation nears")]
    public AudioClip beepSound;
    public float beepIntervalStart = 1.5f;
    public float beepIntervalEnd = 0.2f;
    public AudioClip detonationSound;
    public AudioClip winSound;

    [Header("Win/Lose")]
    public GameObject winPanel;
    [Tooltip("The win screen used to have no buttons at all - the cursor unlocked with nowhere to go.")]
    public Button playAgainButton;
    public Button winMainMenuButton;

    bool roundActive;
    AudioSource audioSource;
    Vector3 chargePosition;
    float disarmProgress;
    readonly List<GameObject> spawned = new List<GameObject>();
    int objectiveRoom = -1;
    string savedObjective;

    public bool IsRoundActive { get { return roundActive; } }

    void Awake()
    {
        Instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        if (AudioManager.Instance != null) audioSource.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
        if (playAgainButton != null) playAgainButton.onClick.AddListener(() => { if (GameOverController.Instance != null) GameOverController.Instance.RestartFromBeginning(); });
        if (winMainMenuButton != null) winMainMenuButton.onClick.AddListener(() => { if (GameOverController.Instance != null) GameOverController.Instance.MainMenu(); });
    }

    public void BeginRound(Vector3 sitePosition)
    {
        if (roundActive) return;
        roundActive = true;
        disarmProgress = 0f;
        // The charge sits wherever the player stood inside the zone, not necessarily the zone's centre.
        chargePosition = device != null ? device.transform.position : sitePosition;
        RefillUtility();
        SetPostPlantObjective();
        StartCoroutine(RunWaves(chargePosition));
        StartCoroutine(RunTimer());
        if (reinforcementPrefab != null && reinforcementSpawns != null)
        {
            for (int i = 0; i < reinforcementSpawns.Length; i++) StartCoroutine(RunReinforcement(reinforcementSpawns[i]));
        }
    }

    void RefillUtility()
    {
        var rm = RoomManager.Instance;
        if (rm == null || rm.abilities == null || plantUtilityRefill <= 0) return;
        Ability[] all = { rm.abilities.reconDart, rm.abilities.smokeWall, rm.abilities.flash, rm.abilities.dash };
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            all[i].AddCharges(plantUtilityRefill);
            all[i].ResetCooldown();
        }
    }

    void SetPostPlantObjective()
    {
        var rm = RoomManager.Instance;
        if (rm == null || rm.CurrentRoom < 0 || string.IsNullOrEmpty(postPlantObjective)) return;
        objectiveRoom = rm.CurrentRoom;
        savedObjective = rm.rooms[objectiveRoom].objective;
        rm.rooms[objectiveRoom].objective = postPlantObjective;
        rm.RefreshObjectiveText();
    }

    void RestoreObjective()
    {
        var rm = RoomManager.Instance;
        if (rm == null || objectiveRoom < 0) return;
        rm.rooms[objectiveRoom].objective = savedObjective;
        objectiveRoom = -1;
        rm.RefreshObjectiveText();
    }

    /// <summary>
    /// Warning light and alarm on the landing spot, then the drone drops in through the open roof and pushes
    /// the charge. The Sentinel and its agent stay off during the drop - an agent can't be moved off the
    /// NavMesh, and a drone shouldn't be able to shoot before it has landed.
    /// </summary>
    IEnumerator RunReinforcement(ReinforcementSpawn spawn)
    {
        float wait = Mathf.Max(0f, spawn.landTime - telegraphSeconds - dropSeconds);
        yield return new WaitForSeconds(wait);
        if (!roundActive) yield break;

        Light warnLight;
        Renderer warnDisc;
        var marker = BuildTelegraph(spawn.position, out warnLight, out warnDisc);
        spawned.Add(marker);
        PlaySfxAt(telegraphSound, spawn.position);

        float t = 0f;
        while (t < telegraphSeconds)
        {
            t += Time.deltaTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 14f);
            warnLight.intensity = Mathf.Lerp(2f, 9f, pulse);
            SetEmission(warnDisc, telegraphColor * Mathf.Lerp(0.8f, 3f, pulse));
            yield return null;
        }
        if (!roundActive) yield break;

        Vector3 toCharge = chargePosition - spawn.position;
        toCharge.y = 0f;
        var rotation = toCharge.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toCharge) : Quaternion.identity;
        var drone = Instantiate(reinforcementPrefab, spawn.position, rotation);
        drone.name = "Sentinel_Reinforcement";
        // Assigned now, not in its Start: Start waits until the drop ends, and other systems (flash, footsteps,
        // reveal) loop over every drone in the meantime.
        if (RoomManager.Instance != null) drone.player = RoomManager.Instance.player;
        spawned.Add(drone.gameObject);
        var agent = drone.GetComponent<NavMeshAgent>();
        drone.enabled = false;
        if (agent != null) agent.enabled = false;
        PlaySfxAt(dropSound, spawn.position);

        Vector3 top = spawn.position + Vector3.up * dropHeight;
        t = 0f;
        while (t < dropSeconds && drone != null && drone.gameObject.activeSelf)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dropSeconds);
            drone.transform.position = Vector3.Lerp(top, spawn.position, 1f - (1f - k) * (1f - k));
            yield return null;
        }
        if (marker != null) Destroy(marker);
        if (drone == null || !drone.gameObject.activeSelf || !roundActive) yield break;

        drone.transform.position = spawn.position;
        if (agent != null) { agent.enabled = true; agent.Warp(spawn.position); }
        drone.enabled = true;
        drone.PushTowards(chargePosition);
    }

    GameObject BuildTelegraph(Vector3 position, out Light light, out Renderer disc)
    {
        var root = new GameObject("ReinforcementWarning");
        root.transform.position = position;

        var discGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        discGo.name = "Disc";
        Destroy(discGo.GetComponent<Collider>());
        discGo.transform.SetParent(root.transform, false);
        discGo.transform.localPosition = new Vector3(0f, 0.01f, 0f);
        discGo.transform.localScale = new Vector3(1.3f, 0.005f, 1.3f);
        disc = discGo.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = Color.black;
        disc.sharedMaterial = mat;

        var lightGo = new GameObject("WarnLight");
        lightGo.transform.SetParent(root.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 3f, 0f);
        lightGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        light = lightGo.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = telegraphColor;
        light.range = 6f;
        light.spotAngle = 50f;
        light.intensity = 2f;
        return root;
    }

    static void SetEmission(Renderer r, Color c)
    {
        if (r == null) return;
        var mat = r.sharedMaterial;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", c);
    }

    /// <summary>
    /// Fills while any living, un-blinded drone is within disarmRadius of the charge, drains slowly when none
    /// is. Blinded drones can't disarm, so a flash on a drone at the charge buys time as well as a kill.
    /// </summary>
    bool UpdateDisarm(out bool disarming)
    {
        disarming = false;
        for (int i = 0; i < spawned.Count && !disarming; i++)
        {
            if (spawned[i] == null || !spawned[i].activeSelf) continue;
            var s = spawned[i].GetComponent<Sentinel>();
            if (s != null && s.enabled && IsDisarming(s)) disarming = true;
        }
        if (siteDefenders != null)
        {
            for (int i = 0; i < siteDefenders.Length && !disarming; i++)
            {
                if (siteDefenders[i] != null && siteDefenders[i].gameObject.activeSelf && IsDisarming(siteDefenders[i])) disarming = true;
            }
        }

        if (disarming) disarmProgress += Time.deltaTime / disarmSeconds;
        else disarmProgress -= Time.deltaTime * disarmDecayPerSecond;
        disarmProgress = Mathf.Clamp01(disarmProgress);
        if (device != null) device.SetDisarmProgress(disarmProgress);
        return disarmProgress >= 1f;
    }

    bool IsDisarming(Sentinel s)
    {
        if (s.IsBlinded) return false;
        Vector3 d = s.transform.position - chargePosition;
        d.y = 0f;
        return d.magnitude <= disarmRadius;
    }

    void LoseToDisarm()
    {
        roundActive = false;
        StopAllCoroutines(); // the calling coroutine stops at its next yield; it yields break right after this
        if (timerPanel != null) timerPanel.SetActive(false);
        if (chargeIcon != null) chargeIcon.SetActive(false);
        if (device != null) device.ShowDisarmed();
        PlaySfx(disarmedSound);
        if (GameOverController.Instance != null) GameOverController.Instance.ShowGameOver(disarmedMessage);
    }

    /// <summary>The blast takes out every drone still in the room - and stops one finishing the player off behind the win screen.</summary>
    void ClearDrones()
    {
        for (int i = 0; i < spawned.Count; i++) if (spawned[i] != null) spawned[i].SetActive(false);
        if (siteDefenders != null)
        {
            for (int i = 0; i < siteDefenders.Length; i++) if (siteDefenders[i] != null) siteDefenders[i].gameObject.SetActive(false);
        }
        if (approachDrones != null)
        {
            for (int i = 0; i < approachDrones.Length; i++) if (approachDrones[i] != null) approachDrones[i].gameObject.SetActive(false);
        }
    }

    void DestroySpawned()
    {
        for (int i = 0; i < spawned.Count; i++) if (spawned[i] != null) Destroy(spawned[i]);
        spawned.Clear();
    }

    IEnumerator RunWaves(Vector3 sitePosition)
    {
        if (siteDefenders == null || siteDefenders.Length == 0) yield break;

        int waveCount = waveDelaysSeconds != null && waveDelaysSeconds.Length > 0 ? waveDelaysSeconds.Length : 1;
        var waves = new List<List<Sentinel>>();
        for (int i = 0; i < waveCount; i++) waves.Add(new List<Sentinel>());
        for (int i = 0; i < siteDefenders.Length; i++)
        {
            waves[i % waveCount].Add(siteDefenders[i]);
        }

        for (int w = 0; w < waveCount; w++)
        {
            float delay = waveDelaysSeconds != null && w < waveDelaysSeconds.Length ? waveDelaysSeconds[w] : 0f;
            yield return new WaitForSeconds(delay - (w > 0 ? waveDelaysSeconds[w - 1] : 0f));
            if (!roundActive) yield break;

            foreach (var s in waves[w])
            {
                if (s != null && s.gameObject.activeSelf) s.PushTowards(sitePosition);
            }
        }
    }

    IEnumerator RunTimer()
    {
        // "CHARGE PLANTED" reads as its own beat before the countdown starts, instead of the
        // timer just silently appearing already ticking.
        if (timerPanel != null) timerPanel.SetActive(true);
        if (timerLabel != null)
        {
            timerLabel.text = "CHARGE PLANTED";
            timerLabel.color = plantedFlashColor;
        }
        if (chargeIcon != null) chargeIcon.SetActive(true);
        yield return new WaitForSeconds(1f);
        if (timerLabel != null) timerLabel.color = timerNormalColor;

        float remaining = detonationSeconds;
        float beepTimer = 0f;
        float disarmTickTimer = 0f;

        while (remaining > 0f && roundActive)
        {
            remaining -= Time.deltaTime;
            float elapsedFrac = 1f - Mathf.Clamp01(remaining / detonationSeconds);
            float beepInterval = Mathf.Lerp(beepIntervalStart, beepIntervalEnd, elapsedFrac);

            beepTimer -= Time.deltaTime;
            if (beepTimer <= 0f)
            {
                PlaySfx(beepSound);
                beepTimer = beepInterval;
            }

            bool disarming;
            if (UpdateDisarm(out disarming))
            {
                LoseToDisarm();
                yield break;
            }
            if (disarming)
            {
                disarmTickTimer -= Time.deltaTime;
                if (disarmTickTimer <= 0f)
                {
                    PlaySfx(disarmTickSound);
                    disarmTickTimer = 0.4f;
                }
            }

            if (timerPanel != null) timerPanel.SetActive(true);
            if (timerLabel != null)
            {
                int secs = Mathf.CeilToInt(Mathf.Max(remaining, 0f));
                if (disarmProgress > 0f)
                {
                    timerLabel.text = "BREACH " + secs + "s  -  DISARMING " + Mathf.RoundToInt(disarmProgress * 100f) + "%";
                    timerLabel.color = disarming ? disarmColor : timerNormalColor;
                }
                else
                {
                    timerLabel.text = "BREACH CHARGE: " + secs + "s";
                    timerLabel.color = timerNormalColor;
                }
            }
            if (device != null) device.SetCountdownProgress(remaining, detonationSeconds);

            yield return null;
        }

        if (roundActive)
        {
            roundActive = false;
            if (timerPanel != null) timerPanel.SetActive(false);
            if (chargeIcon != null) chargeIcon.SetActive(false);
            if (device != null) device.Detonate();
            ClearDrones();
            PlaySfx(detonationSound);
            yield return StartCoroutine(PlayDetonationFx());
            ShowWin();
        }
    }

    IEnumerator PlayDetonationFx()
    {
        if (detonationParticles != null)
        {
            detonationParticles.Play();
        }

        Vector3 shakeOrigin = cameraShakeTransform != null ? cameraShakeTransform.localPosition : Vector3.zero;
        float t = 0f;
        float flashDuration = 0.3f;

        while (t < shakeDuration)
        {
            t += Time.deltaTime;

            if (detonationFlash != null)
            {
                Color c = detonationFlash.color;
                c.a = Mathf.Lerp(0.9f, 0f, Mathf.Clamp01(t / flashDuration));
                detonationFlash.color = c;
            }

            if (cameraShakeTransform != null)
            {
                float falloff = 1f - Mathf.Clamp01(t / shakeDuration);
                cameraShakeTransform.localPosition = shakeOrigin + (Vector3)(UnityEngine.Random.insideUnitCircle * shakeMagnitude * falloff);
            }

            yield return null;
        }

        if (detonationFlash != null)
        {
            var c = detonationFlash.color;
            c.a = 0f;
            detonationFlash.color = c;
        }
        if (cameraShakeTransform != null) cameraShakeTransform.localPosition = shakeOrigin;
    }

/// <summary>Stops the round itself; GameOverController (driven from PlayerHealth.Die) now owns showing the actual Game Over screen for every death, not just this one.</summary>
    public void OnPlayerDied()
    {
        if (!roundActive) return;
        roundActive = false;
        StopAllCoroutines();
        if (timerPanel != null) timerPanel.SetActive(false);
        if (chargeIcon != null) chargeIcon.SetActive(false);
    }

    void PlaySfxAt(AudioClip clip, Vector3 position)
    {
        if (clip != null && AudioManager.Instance != null) AudioManager.Instance.PlaySfxAtPoint(clip, position);
    }

    void PlaySfx(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
    }

    void ShowWin()
    {
        // Stop the clock before the panel opens, so the debrief (RunDebriefUI.OnEnable) reads the final time.
        if (RunStats.Instance != null) RunStats.Instance.Finish();
        if (timerPanel != null) timerPanel.SetActive(false);
        if (chargeIcon != null) chargeIcon.SetActive(false);
        if (winPanel != null) winPanel.transform.SetAsLastSibling();
        if (winPanel != null) winPanel.SetActive(true);
        PlaySfx(winSound);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        // Freeze the world behind the debrief - a surviving drone could otherwise still shoot the player
        // and open Game Over on top of the win screen. Both buttons reset timeScale on their reload.
        Time.timeScale = 0f;
    }



    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }


/// <summary>Used by CheckpointManager on a checkpoint retry - returns the round to its pre-plant idle state, matching a fresh scene load.</summary>
    public void ResetState()
    {
        roundActive = false;
        StopAllCoroutines();
        DestroySpawned();
        disarmProgress = 0f;
        RestoreObjective();
        if (timerPanel != null) timerPanel.SetActive(false);
        if (chargeIcon != null) chargeIcon.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);
        if (device != null) device.ResetState();

        if (detonationFlash != null)
        {
            var c = detonationFlash.color;
            c.a = 0f;
            detonationFlash.color = c;
        }
        if (cameraShakeTransform != null) cameraShakeTransform.localPosition = Vector3.zero;
    }
}
