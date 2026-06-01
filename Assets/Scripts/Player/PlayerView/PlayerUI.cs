using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI promptText;

    [Header("Oxygen UI")] 
    [SerializeField] private Image oxygenFill;
    [SerializeField] private TMP_Text oxygenText;
    
    private static readonly Color ColorSafe   = new Color(0.02f, 0.72f, 0.85f);
    private static readonly Color ColorWarn   = new Color(0.92f, 0.60f, 0.00f);
    private static readonly Color ColorDanger = new Color(0.88f, 0.11f, 0.28f);
    private string currentText;

    public void UpdateText(string promptMessage)
    {
        if (promptMessage == currentText) return;
        currentText = promptMessage;
        promptText.text = promptMessage;
    }

    public void UpdateOxygen(float percent)
    {
        if (oxygenFill == null) return;
        if (oxygenText == null) return;
        
        oxygenFill.fillAmount = percent;
        oxygenText.text = $"{Mathf.RoundToInt(percent)}";

        Color c = percent > 0.6f ? ColorSafe
                : percent > 0.25f ? ColorWarn
                : ColorDanger;
        oxygenFill.color = c;
        oxygenText.color = c;
    }
}
