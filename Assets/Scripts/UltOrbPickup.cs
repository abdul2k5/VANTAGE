using UnityEngine;

/// <summary>Walk into it to collect ultimate charge. Meant to be placed in exposed/risky spots - the charge is a reward for the risk of going to get it.</summary>
public class UltOrbPickup : MonoBehaviour
{
    public float chargeAmount = 50f;
    public float bobSpeed = 2f;
    public float bobHeight = 0.15f;
    public float spinSpeed = 60f;

    Vector3 startPos;
    bool collected;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (collected) return;
        transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player")) return;

        if (UltimateController.Instance != null)
        {
            UltimateController.Instance.AddCharge(chargeAmount);
        }

        collected = true;
        gameObject.SetActive(false);
    }
}
