using UnityEngine;
using TMPro;

public class InteractionPromptUI : MonoBehaviour
{
    public static InteractionPromptUI Instance;

    public TextMeshProUGUI promptText;

    private void Awake()
    {
        Instance = this;
    }

    public void ShowMessage(string message)
    {
        promptText.text = message;
        promptText.gameObject.SetActive(true);
    }

    public void HideMessage()
    {
        promptText.gameObject.SetActive(false);
    }
}
