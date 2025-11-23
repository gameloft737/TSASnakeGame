using UnityEngine;

public class CursorLock : MonoBehaviour
{
    [SerializeField] private bool lockOnStart = true;
    [SerializeField]private WaveManager waveManager;

    void Start()
    {
        
        if (lockOnStart)
        {
            LockCursor();
        }
    }

    void Update()
    {
        // Automatically unlock cursor during choice phase
        if (waveManager != null && waveManager.IsInChoicePhase())
        {
            UnlockCursor();
        }
        else if (lockOnStart)
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

    // Lock cursor when window gains focus (only if not in choice phase)
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && lockOnStart)
        {
            if (waveManager != null && !waveManager.IsInChoicePhase())
            {
                LockCursor();
            }
        }
    }
}