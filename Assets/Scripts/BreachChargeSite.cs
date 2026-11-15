using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Hold the plant key inside this trigger zone to arm the Breach Charge. Cancels on moving,
/// taking damage, releasing the key, or leaving the zone - planting has to actually cost you
/// something, same as every other commitment in this game.
/// </summary>
public class BreachChargeSite : MonoBehaviour
{
    public float plantSeconds = 4f;
    public float moveCancelThreshold = 0.15f;
    public Key plantKey = Key.Digit4;
    public string plantKeyDisplay = "4";

    public GameObject progressRoot;
    public Image progressFill;
    public Text progressLabel;
    public AudioClip plantCompleteSound;
    public BreachChargeDevice device;

    [Header("Charge in hand while planting - positions are from the camera, metres")]
    public float heldChargeScale = 0.32f;
    public Vector3 heldChargeShown = new Vector3(0f, -0.3f, 0.62f);
    [Tooltip("Below the bottom of the screen.")]
    public Vector3 heldChargeHidden = new Vector3(0f, -0.75f, 0.62f);
    public float heldChargePressDown = 0.05f;
    public float heldChargeTwistDegrees = 300f;

    float plantProgress;
    Transform heldCharge;
    bool planted;
    bool playerInside;

    Transform player;
    CharacterController playerCC;
    PlayerHealth playerHealth;
    float lastKnownHealth;

    public bool IsPlanted { get { return planted; } }
    /// <summary>True while actively planting (held, stationary, grounded, inside the zone) - read by PlayerController to block the jump input outright, not just cancel after the fact.</summary>
    public static bool AnyPlantInProgress;


    void Start()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
            playerCC = playerGO.GetComponent<CharacterController>();
            playerHealth = playerGO.GetComponent<PlayerHealth>();
            lastKnownHealth = playerHealth != null ? playerHealth.CurrentHealth : 0f;
        }

        if (progressRoot != null) progressRoot.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = true;
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            // Once planted, stepping off the site must leave the charge where it is - CancelPlant hides the device.
            if (!planted) CancelPlant();
        }
    }

    void Update()
    {
        if (planted) return;
        if (!playerInside || playerHealth == null)
        {
            CancelPlant();
            return;
        }

        bool damagedThisFrame = playerHealth.CurrentHealth < lastKnownHealth;
        lastKnownHealth = playerHealth.CurrentHealth;

        var kb = Keyboard.current;
        bool holding = kb != null && kb[plantKey].isPressed;

        float horizontalSpeed = playerCC != null
            ? new Vector2(playerCC.velocity.x, playerCC.velocity.z).magnitude
            : 0f;
        bool moving = horizontalSpeed > moveCancelThreshold;
        bool airborne = playerCC != null && !playerCC.isGrounded;

        if (!holding || moving || airborne || damagedThisFrame || MainMenuController.IsOpen || Cursor.lockState != CursorLockMode.Locked)
        {
            CancelPlant();
            bool menuOpen = MainMenuController.IsOpen || Cursor.lockState != CursorLockMode.Locked;
            if (!menuOpen) ShowPrompt(holding && (moving || airborne) ? "STAND STILL TO PLANT" : "HOLD [" + plantKeyDisplay + "] TO PLANT THE BREACH CHARGE");
            return;
        }

        AnyPlantInProgress = true;
        if (progressRoot != null) progressRoot.SetActive(true);
        plantProgress += Time.deltaTime;

        float frac = Mathf.Clamp01(plantProgress / plantSeconds);
        if (progressFill != null) progressFill.fillAmount = frac;
        if (progressLabel != null) progressLabel.text = "PLANTING BREACH CHARGE - " + Mathf.CeilToInt((1f - frac) * plantSeconds) + "s";
        if (device != null && player != null) device.SetDeployProgress(player.position, frac);
        ShowHeldCharge(frac);

        if (plantProgress >= plantSeconds)
        {
            Plant();
        }
    }

    /// <summary>Standing on the site without planting: same box as the progress bar, left empty, telling the player the key.</summary>
    void ShowPrompt(string text)
    {
        if (progressRoot != null) progressRoot.SetActive(true);
        if (progressFill != null) progressFill.fillAmount = 0f;
        if (progressLabel != null) progressLabel.text = text;
    }

    /// <summary>
    /// The charge in the player's hands while it is being planted: it comes up from the bottom of the screen
    /// as the gun goes away, is twisted to arm it while being pressed down, and drops out of view as the real
    /// one is set. A small copy of the device's own canister, so its lights come on in step with the real one's.
    /// </summary>
    void ShowHeldCharge(float frac01)
    {
        if (heldCharge == null)
        {
            var cam = Camera.main;
            if (cam == null || device == null || device.canister == null) return;
            heldCharge = Instantiate(device.canister.gameObject, cam.transform).transform;
            heldCharge.name = "HeldBreachCharge";
            heldCharge.localScale = Vector3.one * heldChargeScale;
            foreach (var r in heldCharge.GetComponentsInChildren<Renderer>(true)) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        float enter = Mathf.SmoothStep(0f, 1f, frac01 / 0.12f);
        float exit = Mathf.SmoothStep(0f, 1f, (frac01 - 0.9f) / 0.1f);
        Vector3 pose = Vector3.Lerp(heldChargeHidden, heldChargeShown, enter * (1f - exit));
        pose.y -= heldChargePressDown * frac01;
        heldCharge.localPosition = pose;
        // Tipped towards the player so the core on top can be seen, and turned as it is armed.
        heldCharge.localRotation = Quaternion.Euler(-25f, 0f, 0f) * Quaternion.Euler(0f, heldChargeTwistDegrees * frac01, 0f);
        heldCharge.gameObject.SetActive(true);
    }

    void HideHeldCharge()
    {
        if (heldCharge != null) heldCharge.gameObject.SetActive(false);
    }

    void CancelPlant()
    {
        HideHeldCharge();
        AnyPlantInProgress = false;
        plantProgress = 0f;
        if (progressRoot != null) progressRoot.SetActive(false);
        if (device != null) device.Hide();
    }

    void Plant()
    {
        HideHeldCharge();
        AnyPlantInProgress = false;
        planted = true;
        if (progressRoot != null) progressRoot.SetActive(false);
        if (device != null) device.ConfirmPlanted();
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(plantCompleteSound);

        if (RoundController.Instance != null)
        {
            RoundController.Instance.BeginRound(transform.position);
        }
    }


/// <summary>Used by CheckpointManager on a checkpoint retry - cancels any in-progress plant and clears the planted flag so the site can be planted again.</summary>
    public void ResetState()
    {
        HideHeldCharge();
        AnyPlantInProgress = false;
        planted = false;
        plantProgress = 0f;
        playerInside = false;
        if (progressRoot != null) progressRoot.SetActive(false);
        if (device != null) device.Hide();
    }
}
