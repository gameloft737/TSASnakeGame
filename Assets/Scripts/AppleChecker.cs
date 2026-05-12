using UnityEngine;

public class AppleChecker : MonoBehaviour
{
    public bool isTouching = false;

    // Cache the Snake layer once. Previously LayerMask.NameToLayer("Snake") was
    // called on every trigger callback — it's a managed string lookup that shows
    // up in WebGL profiles with many colliders.
    private static int _snakeLayer = -1;

    private static int SnakeLayer
    {
        get
        {
            if (_snakeLayer < 0) _snakeLayer = LayerMask.NameToLayer("Snake");
            return _snakeLayer;
        }
    }

    void Awake()
    {
        isTouching = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == SnakeLayer)
        {
            isTouching = true;
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == SnakeLayer)
        {
            isTouching = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == SnakeLayer)
        {
            isTouching = false;
        }
    }
}
