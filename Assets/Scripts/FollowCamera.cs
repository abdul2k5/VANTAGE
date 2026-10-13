using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.15f;
    public float lookAheadDistance = 1.5f;

    Vector3 velocity;

void LateUpdate()
    {
        if (target == null) return;

        Vector3 lookAhead = target.right * lookAheadDistance;
        Vector3 desired = new Vector3(target.position.x + lookAhead.x, target.position.y + lookAhead.y, transform.position.z);
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }


public void SnapToTarget()
    {
        if (target == null) return;
        Vector3 lookAhead = target.right * lookAheadDistance;
        transform.position = new Vector3(target.position.x + lookAhead.x, target.position.y + lookAhead.y, transform.position.z);
        velocity = Vector3.zero;
    }
}
