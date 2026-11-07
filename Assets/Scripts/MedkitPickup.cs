using UnityEngine;

/// <summary>Walk into it to recover health. Placed in exposed spots, so healing costs a risk. Isn't used up at full health, and picks up as soon as a player standing on it gets hurt.</summary>
public class MedkitPickup : MonoBehaviour
{
    public float healAmount = 50f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.1f;
    public float spinSpeed = 60f;
    public AudioClip pickupSound;
    /// <summary>Logical RoomManager index this medkit sits in - checkpoint retry respawns medkits from the checkpoint room onward.</summary>
    public int roomIndex;

    Vector3 startPos;
    bool collected;
    PlayerHealth playerInside;

    void Start()
    {
        startPos = transform.position;
    }

    /// <summary>Checkpoint retry: put a collected medkit back, so the retry matches the first arrival at the checkpoint.</summary>
    public void ResetState()
    {
        collected = false;
        playerInside = null;
        transform.position = startPos;
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (collected) return;
        transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

        if (playerInside != null && playerInside.CurrentHealth < playerInside.MaxHealth && !playerInside.AttemptOver) Collect();
    }

    void Collect()
    {
        playerInside.Heal(healAmount);
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(pickupSound);

        collected = true;
        gameObject.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = other.GetComponent<PlayerHealth>();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = null;
    }
}
