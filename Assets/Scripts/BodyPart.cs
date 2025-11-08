using UnityEngine;

public class BodyPart : MonoBehaviour
{
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public void FollowTarget(Vector3 targetPos, Quaternion targetRot)
    {
        rb.MovePosition(targetPos);
        rb.MoveRotation(targetRot);
    }
}