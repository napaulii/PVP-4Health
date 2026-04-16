using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ThemeChange : MonoBehaviour
{
    [Header("Apatinis - Light, Virsutinis - Dark, jeigu nieko - tuscia")]
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    [Header("Apatinis - Light, Virsutinis - Dark, jeigu nieko - tuscia")]
    [SerializeField] private TMP_Text targetText;
    [SerializeField] private Color onColor = Color.white;
    [SerializeField] private Color offColor = Color.gray;

    private void OnEnable() => GlobalToggle.OnToggled += OnToggleChanged;
    private void OnDisable() => GlobalToggle.OnToggled -= OnToggleChanged;

    private void OnToggleChanged(bool isOn)
    {
        if (targetImage != null)
            targetImage.sprite = isOn ? onSprite : offSprite;

        if (targetText != null)
            targetText.color = isOn ? onColor : offColor;
    }
}