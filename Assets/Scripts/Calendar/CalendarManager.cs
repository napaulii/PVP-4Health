using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;
using System.Linq;

public class CalendarManager : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI monthYearText;

    [Header("Grid")]
    [SerializeField] private Transform calendarGrid;
    [SerializeField] private GameObject calendarDayPrefab;

    [Header("Detail Panel")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI dateTitle;
    [SerializeField] private Transform itemListContent;
    [SerializeField] private GameObject taskItemPrefab;
    [SerializeField] private GameObject emptyText;

    private CalendarDataLoader dataLoader;
    private CalendarGridBuilder gridBuilder;
    private CalendarDetailPanel detailPanelManager;

    private int currentYear;
    private int currentMonth;
    private bool isSupabaseReady = false;

    async void Start()
    {
        currentYear = DateTime.Now.Year;
        currentMonth = DateTime.Now.Month;

        dataLoader = new CalendarDataLoader();
        gridBuilder = new CalendarGridBuilder(calendarGrid, calendarDayPrefab);
        detailPanelManager = new CalendarDetailPanel(
            detailPanel, dateTitle, itemListContent, taskItemPrefab, emptyText);

        if (prevButton != null) prevButton.onClick.AddListener(PreviousMonth);
        if (nextButton != null) nextButton.onClick.AddListener(NextMonth);

        SetupCloseButton();

        if (detailPanel != null) detailPanel.SetActive(false);

        await InitializeSupabaseAndLoadData();
    }

    // ─── Close Button ─────────────────────────────────────────────────────────

    private void SetupCloseButton()
    {
        if (closeButton == null)
        {
            var found = FindObjectsByType<Button>(FindObjectsSortMode.None)
                            .FirstOrDefault(b => b.name == "CloseButton");
            closeButton = found;

            if (closeButton != null) Debug.Log("CloseButton auto-found!");
            else Debug.LogError("CloseButton not found! Assign it in Inspector.");
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() =>
            {
                if (detailPanel != null) detailPanel.SetActive(false);
            });
        }
    }

    // ─── Supabase Init ────────────────────────────────────────────────────────

    private async Task InitializeSupabaseAndLoadData()
    {
        int attempts = 0;
        while (SupabaseManager.Instance == null)
        {
            await Task.Delay(200);
            if (++attempts >= 50)
            {
                Debug.LogError("SupabaseManager not found");
                return;
            }
        }

        // Check if user is already logged in (from login scene)
        if (SupabaseManager.Instance.Auth?.CurrentUser == null)
        {
            Debug.LogWarning("No user logged in. Please login from the login scene first.");
            return;
        }

        Debug.Log($"Logged in as: {SupabaseManager.Instance.Auth.CurrentUser.Email}");
        isSupabaseReady = true;

        await dataLoader.EnsureAuthenticated();
        await dataLoader.LoadCompletedDays();
        BuildCalendar();
    }

    // ─── Calendar Build ───────────────────────────────────────────────────────

    private void BuildCalendar()
    {
        UpdateMonthYearText();
        gridBuilder.Generate(currentYear, currentMonth, dataLoader.CompletedDays, OnDayCellClicked);
    }

    private async void OnDayCellClicked(DateTime date, bool isCompleted)
    {
        await detailPanelManager.Show(date, dataLoader);
    }

    // ─── Navigation ───────────────────────────────────────────────────────────

    public async void NextMonth()
    {
        currentMonth++;
        if (currentMonth > 12) { currentMonth = 1; currentYear++; }
        if (isSupabaseReady) await dataLoader.LoadCompletedDays();
        BuildCalendar();
    }

    public async void PreviousMonth()
    {
        currentMonth--;
        if (currentMonth < 1) { currentMonth = 12; currentYear--; }
        if (isSupabaseReady) await dataLoader.LoadCompletedDays();
        BuildCalendar();
    }

    private void UpdateMonthYearText()
    {
        if (monthYearText == null) return;
        string[] months =
        {
            "", "January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December"
        };
        monthYearText.text = $"{months[currentMonth]} {currentYear}";
    }
}