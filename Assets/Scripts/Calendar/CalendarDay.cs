using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Attach this to CalendarDayPrefab root.
/// Expects: one Image (background), one TextMeshProUGUI (day number),
/// optionally a child named "Dot" (completion indicator).
/// </summary>
public class CalendarDay : MonoBehaviour
{
    // Assign in Prefab Inspector — or auto-found below
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private GameObject dot;         // optional completion dot child

    public event Action<DateTime, bool> OnDayClicked;

    private static readonly Color ColorMint = HexColor("D1F6C1");
    private static readonly Color ColorTeal = HexColor("4AB7B7");
    private static readonly Color ColorCoral = HexColor("F57665");

    private DateTime date;
    private bool isCompleted;

    // ─── Public Init ─────────────────────────────────────────────────────────

    public void Initialize(DateTime date, bool isCompleted)
    {
        this.date = date;
        this.isCompleted = isCompleted;

        AutoFindReferences();
        ApplyVisuals();
        SetupButton();
    }

    // ─── Setup ───────────────────────────────────────────────────────────────

    private void AutoFindReferences()
    {
        if (background == null) background = GetComponent<Image>();
        if (dayText == null) dayText = GetComponentInChildren<TextMeshProUGUI>();
        if (dot == null)
        {
            Transform dotTransform = transform.Find("Dot");
            if (dotTransform != null) dot = dotTransform.gameObject;
        }
    }

    private void ApplyVisuals()
    {
        // Background color
        if (background != null)
        {
            if (date.Date == DateTime.Today) background.color = ColorMint;
            else if (isCompleted) background.color = new Color(ColorTeal.r, ColorTeal.g, ColorTeal.b, 0.4f);
            else if (date.Date > DateTime.Today) background.color = new Color(0.85f, 0.85f, 0.85f, 0.25f);
            else background.color = new Color(ColorCoral.r, ColorCoral.g, ColorCoral.b, 0.25f);
        }

        // Day number
        if (dayText != null)
            dayText.text = date.Day.ToString();

        // Completion dot
        if (dot != null)
            dot.SetActive(isCompleted);
    }

    private void SetupButton()
    {
        Button btn = GetComponent<Button>();
        if (btn == null) btn = gameObject.AddComponent<Button>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnDayClicked?.Invoke(date, isCompleted));
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

    private static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}