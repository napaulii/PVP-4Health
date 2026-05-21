using UnityEngine;
using System;
using System.Collections.Generic;

public class CalendarGridBuilder
{
    private readonly Transform calendarGrid;
    private readonly GameObject calendarDayPrefab;

    public CalendarGridBuilder(Transform calendarGrid, GameObject calendarDayPrefab)
    {
        this.calendarGrid = calendarGrid;
        this.calendarDayPrefab = calendarDayPrefab;
    }

    public void Generate(
        int year, int month,
        HashSet<DateTime> completedDays,
        Action<DateTime, bool> onDayClicked)
    {
        if (calendarGrid == null)
        {
            Debug.LogError("CalendarGrid not assigned!");
            return;
        }

        if (calendarDayPrefab == null)
        {
            Debug.LogError("CalendarDayPrefab not assigned!");
            return;
        }

        // Clear existing cells
        for (int i = calendarGrid.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(calendarGrid.GetChild(i).gameObject);

        DateTime firstDay = new DateTime(year, month, 1);
        int daysInMonth = DateTime.DaysInMonth(year, month);
        int startOffset = ((int)firstDay.DayOfWeek + 6) % 7; // Monday = 0

        int totalCells = startOffset + daysInMonth;

        for (int i = 0; i < totalCells; i++)
        {
            if (i < startOffset)
            {
                // Empty placeholder — instantiate prefab but hide/disable it
                GameObject empty = UnityEngine.Object.Instantiate(calendarDayPrefab, calendarGrid);
                empty.name = "Empty";

                // Hide visuals but keep layout slot
                var images = empty.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                var texts = empty.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                var buttons = empty.GetComponentsInChildren<UnityEngine.UI.Button>(true);

                foreach (var img in images) img.color = Color.clear;
                foreach (var txt in texts) txt.text = "";
                foreach (var btn in buttons) btn.interactable = false;
            }
            else
            {
                int day = i - startOffset + 1;
                DateTime date = new DateTime(year, month, day);
                bool isCompleted = completedDays.Contains(date.Date);

                GameObject cell = UnityEngine.Object.Instantiate(calendarDayPrefab, calendarGrid);
                cell.name = $"Day_{day:00}";

                CalendarDay script = cell.GetComponent<CalendarDay>();
                if (script == null) script = cell.AddComponent<CalendarDay>();

                script.Initialize(date, isCompleted);
                script.OnDayClicked += onDayClicked;
            }
        }

        Canvas.ForceUpdateCanvases();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(calendarGrid as RectTransform);
    }
}