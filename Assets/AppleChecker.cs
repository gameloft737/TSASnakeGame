using UnityEngine;

public class AppleChecker : MonoBehaviour
{
    public bool isTouching = false;

    void Awake()
    {
        isTouching = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Snake"))
        {
            isTouching = true;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Snake"))
        {
            isTouching = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Snake"))
        {
            isTouching = false;
        }
    }
}