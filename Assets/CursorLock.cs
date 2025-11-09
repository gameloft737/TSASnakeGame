using UnityEngine;

public class CursorLock : MonoBehaviour
{
    [SerializeField] private bool lockOnStart = true;

    void Start()
    {
        if (lockOnStart)
        {
            LockCursor();
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Lock cursor when window gains focus
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && lockOnStart)
        {
            LockCursor();
        }
    }
}