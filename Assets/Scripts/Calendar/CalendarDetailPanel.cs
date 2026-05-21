using UnityEngine;
using TMPro;
using System;
using System.Threading.Tasks;

/// <summary>
/// Manages the detail panel (DetailPanel > Panel > ItemList > Content).
/// Uses TaskItemPrefab to display completed habits for the selected day.
/// TaskItemPrefab must have a TextMeshProUGUI somewhere in its hierarchy.
/// </summary>
public class CalendarDetailPanel
{
    private readonly GameObject detailPanel;
    private readonly TextMeshProUGUI dateTitle;
    private readonly Transform itemListContent;
    private readonly GameObject taskItemPrefab;
    private readonly GameObject emptyText;

    private static readonly Color ColorTeal = HexColor("4AB7B7");

    public CalendarDetailPanel(
        GameObject detailPanel,
        TextMeshProUGUI dateTitle,
        Transform itemListContent,
        GameObject taskItemPrefab,
        GameObject emptyText)
    {
        this.detailPanel = detailPanel;
        this.dateTitle = dateTitle;
        this.itemListContent = itemListContent;
        this.taskItemPrefab = taskItemPrefab;
        this.emptyText = emptyText;
    }

    // ─── Public ───────────────────────────────────────────────────────────────

    public async Task Show(DateTime date, CalendarDataLoader dataLoader)
    {
        if (dateTitle != null)
            dateTitle.text = date.ToString("yyyy-MM-dd, dddd");

        await PopulateHabits(date, dataLoader);

        if (detailPanel != null)
            detailPanel.SetActive(true);
    }

    public void Hide()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    // ─── Private ─────────────────────────────────────────────────────────────

    private async Task PopulateHabits(DateTime date, CalendarDataLoader dataLoader)
    {
        // Clear old items
        if (itemListContent != null)
            foreach (Transform child in itemListContent)
                UnityEngine.Object.Destroy(child.gameObject);

        var habits = await dataLoader.GetHabitsForDate(date);

        if (emptyText != null)
            emptyText.SetActive(habits.Count == 0);

        if (itemListContent == null || taskItemPrefab == null) return;

        foreach (var habit in habits)
        {
            GameObject item = UnityEngine.Object.Instantiate(taskItemPrefab, itemListContent);
            item.name = "TaskItem_" + habit.Title;

            // Find TMP label in the prefab and set text
            TextMeshProUGUI label = item.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = $"✅  {habit.Title}";
                label.color = ColorTeal;
            }
            else
            {
                Debug.LogWarning($"TaskItemPrefab has no TextMeshProUGUI for habit: {habit.Title}");
            }
        }
    }

    private static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}