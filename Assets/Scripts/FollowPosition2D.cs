using UnityEngine;

public class FollowPosition2D : MonoBehaviour
{
    public Transform target;
    public Vector2 offset;

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
    }
}
