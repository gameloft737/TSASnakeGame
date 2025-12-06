using UnityEngine;
using UnityEngine.Events;

public class CutsceneManager : MonoBehaviour
{
    public UnityEvent OnCutsceneFinished; // Assign things to start AFTER cutscene

    public void CutsceneDone()
    {
        OnCutsceneFinished?.Invoke();
    }
}
