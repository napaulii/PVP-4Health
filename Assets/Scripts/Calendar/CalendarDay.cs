using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CalendarDay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image completedMarker;
    private Button button;

    private DateTime date;
    private bool isCompleted;

    public event Action<DateTime, bool> OnDayClicked;

    // Tavo spalvų paletė
    private static readonly Color ColorMint = HexColor("D1F6C1");
    private static readonly Color ColorTeal = HexColor("4AB7B7");
    private static readonly Color ColorCoral = HexColor("F57665");
    private static readonly Color ColorEmpty = new Color(0.85f, 0.85f, 0.85f, 0.3f);

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (dayText == null)
            dayText = GetComponentInChildren<TextMeshProUGUI>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        // Užkrauname Russo One šriftą
        ApplyRussoFont();

        if (completedMarker == null)
        {
            GameObject marker = new GameObject("CompletedMarker");
            marker.transform.SetParent(transform, false);
            completedMarker = marker.AddComponent<Image>();
            completedMarker.color = ColorTeal;

            RectTransform mr = marker.GetComponent<RectTransform>();
            mr.sizeDelta = new Vector2(8, 8);
            mr.anchorMin = new Vector2(0.5f, 0f);
            mr.anchorMax = new Vector2(0.5f, 0f);
            mr.anchoredPosition = new Vector2(0f, 8f);

            completedMarker.gameObject.SetActive(false);
        }
    }

    private void ApplyRussoFont()
    {
        // Bandome rasti RussoOne-Regular SDF assets aplanke
        TMP_FontAsset russoFont = Resources.Load<TMP_FontAsset>("Fonts/RussoOne-Regular SDF");

        // Jei neranda Resources/Fonts/ — bandome per TMP Settings
        if (russoFont == null)
            russoFont = TMP_Settings.defaultFontAsset;

        if (dayText != null && russoFont != null)
        {
            dayText.font = russoFont;
            Debug.Log("✅ Russo One font applied to CalendarDay");
        }
        else if (russoFont == null)
        {
            Debug.LogWarning("⚠️ RussoOne-Regular SDF not found in Resources/Fonts/ — using default font");
        }
    }

    private void OnEnable()
    {
        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(HandleClick);
    }

    public void Initialize(DateTime date, bool isCompleted)
    {
        this.date = date;
        this.isCompleted = isCompleted;

        if (dayText != null)
        {
            dayText.text = date.Day.ToString();
            dayText.fontSize = 20;
            dayText.alignment = TextAlignmentOptions.Center;
            dayText.color = new Color(0.15f, 0.15f, 0.15f);
        }

        // Spalva pagal statusą
        if (backgroundImage != null)
        {
            if (date.Date == DateTime.Today)
                backgroundImage.color = ColorMint;
            else if (isCompleted)
                backgroundImage.color = new Color(ColorTeal.r, ColorTeal.g, ColorTeal.b, 0.35f);
            else if (date.Date > DateTime.Today)
                backgroundImage.color = ColorEmpty;
            else
                backgroundImage.color = new Color(ColorCoral.r, ColorCoral.g, ColorCoral.b, 0.25f);
        }

        if (completedMarker != null)
            completedMarker.gameObject.SetActive(isCompleted);

        if (button != null)
            button.interactable = true;
    }

    private void HandleClick()
    {
        OnDayClicked?.Invoke(date, isCompleted);
    }

    private static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}