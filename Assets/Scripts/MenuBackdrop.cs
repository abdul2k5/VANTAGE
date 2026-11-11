using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The live 3D scene behind the main menu: a second camera drifts slowly through the Range while a Sentinel
/// (visuals only - no AI, no collider) patrols in front of the targets. The camera renders into a texture
/// shown by a RawImage at the back of the menu panel, so the HUD underneath the menu stays covered.
/// The menu holds Time.timeScale at 0, so everything here runs on unscaled time. Once the game starts the
/// camera, the drone and the texture are all switched off - none of it exists during play.
/// </summary>
public class MenuBackdrop : MonoBehaviour
{
    public Camera backdropCamera;
    public RawImage target;

    [Header("Camera move - eases back and forth between the two ends")]
    public Vector3 cameraStart = new Vector3(-1.6f, 1.45f, -5.6f);
    public Vector3 cameraEnd = new Vector3(3.8f, 1.7f, -5.0f);
    [Tooltip("Left of the room's centre, so the scene sits in the right two thirds of the screen, clear of the menu column.")]
    public Vector3 lookAt = new Vector3(-1.6f, 1.2f, 1.2f);
    public float cameraSeconds = 28f;

    [Header("Patrolling Sentinel")]
    public Transform drone;
    public Vector3 patrolA = new Vector3(-0.8f, 0f, -1.8f);
    public Vector3 patrolB = new Vector3(4.2f, 0f, -1.2f);
    public float patrolSpeed = 1.1f;
    public float turnSpeed = 2.2f;
    public float pauseSeconds = 1.6f;
    public float bobHeight = 0.05f;
    public float bobSpeed = 1.7f;

    RenderTexture texture;
    bool toB = true;
    float pauseLeft;
    float droneBaseY;

    void Start()
    {
        // Restart / Play Again skip the menu, so there is nothing to show.
        if (!MainMenuController.IsOpen)
        {
            ShutDown();
            return;
        }

        texture = new RenderTexture(Screen.width, Screen.height, 24);
        texture.name = "MenuBackdropTexture";
        if (backdropCamera != null)
        {
            backdropCamera.targetTexture = texture;
            backdropCamera.enabled = true;
        }
        if (target != null)
        {
            target.texture = texture;
            target.enabled = true;
        }
        if (drone != null)
        {
            drone.gameObject.SetActive(true);
            droneBaseY = drone.position.y;
            drone.position = new Vector3(patrolA.x, droneBaseY, patrolA.z);
            drone.rotation = Quaternion.LookRotation(Flat(patrolB - patrolA));
        }
        MoveCamera();
    }

    void Update()
    {
        if (!MainMenuController.IsOpen)
        {
            ShutDown();
            return;
        }

        MoveCamera();
        MoveDrone(Time.unscaledDeltaTime);
    }

    void MoveCamera()
    {
        if (backdropCamera == null) return;
        // Cosine ease: slows to a stop at each end instead of bouncing off it.
        float k = 0.5f - 0.5f * Mathf.Cos(Time.unscaledTime / cameraSeconds * Mathf.PI * 2f);
        var t = backdropCamera.transform;
        t.position = Vector3.Lerp(cameraStart, cameraEnd, k);
        t.rotation = Quaternion.LookRotation(lookAt - t.position);
    }

    /// <summary>Walks to the far point, stops and turns to look down the range at the camera, then walks back.</summary>
    void MoveDrone(float dt)
    {
        if (drone == null) return;

        Vector3 goal = toB ? patrolB : patrolA;
        Vector3 pos = drone.position;
        Vector3 to = Flat(goal - pos);
        Vector3 face;

        if (pauseLeft > 0f)
        {
            pauseLeft -= dt;
            face = backdropCamera != null ? Flat(backdropCamera.transform.position - pos) : drone.forward;
        }
        else if (to.magnitude < 0.05f)
        {
            toB = !toB;
            pauseLeft = pauseSeconds;
            face = drone.forward;
        }
        else
        {
            pos += to.normalized * Mathf.Min(patrolSpeed * dt, to.magnitude);
            face = to;
        }

        pos.y = droneBaseY + Mathf.Sin(Time.unscaledTime * bobSpeed) * bobHeight;
        drone.position = pos;
        if (face.sqrMagnitude > 0.0001f)
            drone.rotation = Quaternion.Slerp(drone.rotation, Quaternion.LookRotation(face.normalized), turnSpeed * dt);
    }

    static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    void ShutDown()
    {
        if (backdropCamera != null)
        {
            backdropCamera.enabled = false;
            backdropCamera.targetTexture = null;
        }
        if (target != null) target.texture = null;
        if (drone != null) drone.gameObject.SetActive(false);
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
            texture = null;
        }
        enabled = false;
    }

    void OnDestroy()
    {
        if (texture != null) texture.Release();
    }
}
