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

    public GameObject progressRoot;
    public Image progressFill;
    public Text progressLabel;
    public AudioClip plantCompleteSound;

    float plantProgress;
    bool planted;
    bool playerInside;

    Transform player;
    CharacterController playerCC;
    PlayerHealth playerHealth;
    float lastKnownHealth;

    public bool IsPlanted { get { return planted; } }

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
            CancelPlant();
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

        if (!holding || moving || damagedThisFrame || MainMenuController.IsOpen || Cursor.lockState != CursorLockMode.Locked)
        {
            CancelPlant();
            return;
        }

        if (progressRoot != null) progressRoot.SetActive(true);
        plantProgress += Time.deltaTime;

        float frac = Mathf.Clamp01(plantProgress / plantSeconds);
        if (progressFill != null) progressFill.fillAmount = frac;
        if (progressLabel != null) progressLabel.text = "PLANTING BREACH CHARGE - " + Mathf.CeilToInt((1f - frac) * plantSeconds) + "s";

        if (plantProgress >= plantSeconds)
        {
            Plant();
        }
    }

    void CancelPlant()
    {
        plantProgress = 0f;
        if (progressRoot != null) progressRoot.SetActive(false);
    }

    void Plant()
    {
        planted = true;
        if (progressRoot != null) progressRoot.SetActive(false);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(plantCompleteSound);

        if (RoundController.Instance != null)
        {
            RoundController.Instance.BeginRound(transform.position);
        }
    }
}
