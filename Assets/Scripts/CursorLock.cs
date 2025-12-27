using UnityEngine;

public class CursorLock : MonoBehaviour
{
    [SerializeField] private bool lockOnStart = true;
    [SerializeField] private WaveManager waveManager; // Reference to the WaveManager (or any other system managing selection phases)
    
    // New variables for ability selection
    [SerializeField] private bool isAbilitySelection = false; // A flag to track if the player is in the ability selection phase

    void Start()
    {
        // Lock the cursor on start if needed
        if (lockOnStart)
        {
            LockCursor();
        }
    }

    void Update()
    {
        // Check if in ability selection phase (if you have a phase manager or similar system)
        if (isAbilitySelection)
        {
            UnlockCursor();
            lockOnStart = false;
        }
        else if (waveManager != null && waveManager.IsInChoicePhase()) // Assuming this is a similar system for attack/ability selection
        {
            UnlockCursor(); // Unlock cursor during attack/ability selection
        }
        else if (lockOnStart)
        {
            LockCursor(); // Lock the cursor again after selection phase
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

    // Call this method to start ability selection phase externally
    public void StartAbilitySelection()
    {
        isAbilitySelection = true; // Set the flag to true
        UnlockCursor(); // Immediately unlock the cursor for ability selection
        Debug.Log("locked");
    }

    // Call this method to stop ability selection and lock cursor again
    public void StopAbilitySelection()
    {
        isAbilitySelection = false; // Set the flag to false
        LockCursor(); // Lock the cursor again after ability selection ends
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
