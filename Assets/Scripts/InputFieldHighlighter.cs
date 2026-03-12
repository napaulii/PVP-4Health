// Highlight valid fields with green border
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InputFieldHighlighter : MonoBehaviour
{
    public TMP_InputField inputField;
    public Image backgroundImage;
    public Color validColor = new Color(0.8f, 1f, 0.8f);
    public Color invalidColor = new Color(1f, 0.8f, 0.8f);
    public Color normalColor = Color.white;

    private HabitValidator validator;

    private void Start()
    {
        validator = GetComponentInParent<HabitValidator>();
        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(OnValueChanged);
        }
    }

    private void OnValueChanged(string value)
    {
        if (backgroundImage == null) return;

        // Check if this field has any errors
        bool hasErrors = CheckForErrors();

        if (hasErrors)
        {
            backgroundImage.color = invalidColor;
        }
        else if (!string.IsNullOrEmpty(value))
        {
            backgroundImage.color = validColor;
        }
        else
        {
            backgroundImage.color = normalColor;
        }
    }

    private bool CheckForErrors()
    {
        // Implement based on which field this is
        // This would check the validator's error panels
        return false;
    }
}