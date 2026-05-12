using UnityEngine;

/// <summary>
/// A proxy transform that smoothly follows a target. Useful as the LookAt
/// target for a CinemachineHardLookAt camera so the camera gets smooth
/// aim without any drift / dead zone wobble.
/// </summary>
public class SmoothLookTarget : MonoBehaviour
{
    [Tooltip("The transform this proxy will smoothly follow (e.g. the snake's orientation child).")]
    [SerializeField] private Transform target;

    [Tooltip("Approximate seconds it takes to catch up to the target. Lower = snappier, higher = floatier.")]
    [SerializeField] private float smoothTime = 0.08f;

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null) return;
        transform.position = Vector3.SmoothDamp(transform.position, target.position, ref velocity, smoothTime);
    }
}
