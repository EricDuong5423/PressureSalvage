using UnityEngine;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI promptText;
    
    private string currentText;

    public void UpdateText(string promptMessage)
    {
        if (promptMessage == currentText) return;
        currentText = promptMessage;
        promptText.text = promptMessage;
    }
}
