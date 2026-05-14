using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using SupabaseModels;

public class CalendarManager : MonoBehaviour
{
    [Header("Header References")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI monthYearText;

    [Header("Calendar Grid")]
    [SerializeField] private Transform calendarGrid;

    [Header("Detail Panel")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI dateTitle;
    [SerializeField] private Transform itemListContent;
    [SerializeField] private GameObject emptyText;

    [Header("Login Credentials")]
    [SerializeField] private string testEmail = "testuser@game.com";
    [SerializeField] private string testPassword = "password123";

    private HabitController habitController;
    private UserChallengeController userChallengeController;

    private int currentYear;
    private int currentMonth;
    private HashSet<DateTime> completedDays = new HashSet<DateTime>();
    private bool isSupabaseReady = false;

    private static readonly Color ColorMint = HexColor("D1F6C1");
    private static readonly Color ColorTeal = HexColor("4AB7B7");
    private static readonly Color ColorCoral = HexColor("F57665");

    private TMP_FontAsset russoFont;

    // ─────────────────────────────────────────
    //  LIFECYCLE
    // ─────────────────────────────────────────

    async void Start()
    {
        LoadRussoFont();

        habitController = new HabitController();
        userChallengeController = new UserChallengeController();

        currentYear = DateTime.Now.Year;
        currentMonth = DateTime.Now.Month;

        if (prevButton != null) prevButton.onClick.AddListener(PreviousMonth);
        if (nextButton != null) nextButton.onClick.AddListener(NextMonth);

        // 🔴 PATIKRINIMAS IR PRISKYRIMAS CLOSE BUTTON
        if (closeButton == null)
        {
            // Bandom rasti CloseButton scenoje
            closeButton = GetComponentInChildren<Button>();
            if (closeButton == null)
            {
                var found = FindObjectsOfType<Button>().FirstOrDefault(b => b.name == "CloseButton");
                closeButton = found;
            }

            if (closeButton != null)
                Debug.Log("✅ CloseButton auto-found!");
            else
                Debug.LogError("❌ CloseButton not found! Please assign it manually in Inspector.");
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => {
                Debug.Log("🔴 Close button clicked!");
                if (detailPanel != null)
                    detailPanel.SetActive(false);
            });
        }

        if (detailPanel != null) detailPanel.SetActive(false);

        ApplyRussoToHeader();

        await InitializeSupabaseAndLoadData();
    }

    // ─────────────────────────────────────────
    //  FONT - RUSSO ONE
    // ─────────────────────────────────────────

    private void LoadRussoFont()
    {
        // Pabandome rasti Resources folderyje
        russoFont = Resources.Load<TMP_FontAsset>("Fonts/RussoOne-Regular SDF");

        // Jei nerado - ieškome visuose Resources
        if (russoFont == null)
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            russoFont = fonts.FirstOrDefault(f => f.name.Contains("RussoOne") || f.name.Contains("Russo"));
        }

        // Jei vis tiek nėra - bandom rasti bet kokį fontą
        if (russoFont == null)
        {
            var anyFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault();
            russoFont = anyFont ?? TMP_Settings.defaultFontAsset;
            Debug.LogWarning("⚠️ RussoOne font not found, using default font");
        }
        else
        {
            Debug.Log($"✅ RussoOne font loaded: {russoFont.name}");
        }
    }

    private void ApplyRussoToHeader()
    {
        if (russoFont == null) return;
        if (monthYearText != null)
        {
            monthYearText.font = russoFont;
            monthYearText.fontSize = 24;
        }
        if (dateTitle != null)
        {
            dateTitle.font = russoFont;
            dateTitle.fontSize = 20;
        }
    }

    private void ApplyRussoToText(TextMeshProUGUI text)
    {
        if (text != null && russoFont != null)
        {
            text.font = russoFont;
        }
    }

    // ─────────────────────────────────────────
    //  CALENDAR GENERATION
    // ─────────────────────────────────────────

    private void GenerateCalendar()
    {
        if (calendarGrid == null) { Debug.LogError("❌ CalendarGrid not assigned!"); return; }

        // Išvalom visus vaikus
        for (int i = calendarGrid.childCount - 1; i >= 0; i--)
        {
            Transform child = calendarGrid.GetChild(i);
            if (child != null)
                DestroyImmediate(child.gameObject);
        }

        // Nustatom GridLayoutGroup su tavo dydžiais
        GridLayoutGroup grid = calendarGrid.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = calendarGrid.gameObject.AddComponent<GridLayoutGroup>();

        // TAVO DYDŽIAI
        grid.cellSize = new Vector2(150, 120);     // Plotis 150, Aukštis 120
        grid.spacing = new Vector2(5, 5);          // Tarpai tarp celių
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 7;                  // 7 dienos per savaitę
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.padding = new RectOffset(5, 5, 5, 5); // Padding aplink

        // Nustatom CalendarGrid plotį
        RectTransform gridRect = calendarGrid as RectTransform;
        if (gridRect != null)
        {
            // 7 celės po 150 + 6 tarpai po 5 + padding kairė/dešinė (5+5)
            float totalWidth = (7 * 150) + (6 * 5) + 10;
            gridRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, totalWidth);
            gridRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 400);
            Debug.Log($"📐 Grid width set to: {totalWidth}");
        }

        DateTime firstDay = new DateTime(currentYear, currentMonth, 1);
        int daysInMonth = DateTime.DaysInMonth(currentYear, currentMonth);
        int startOffset = ((int)firstDay.DayOfWeek + 6) % 7;

        int totalCells = startOffset + daysInMonth;
        Debug.Log($"📅 Generating {totalCells} cells ({startOffset} empty + {daysInMonth} days)");

        for (int i = 0; i < totalCells; i++)
        {
            if (i < startOffset)
            {
                CreateEmptyCell();
            }
            else
            {
                int day = i - startOffset + 1;
                DateTime date = new DateTime(currentYear, currentMonth, day);
                bool isCompleted = completedDays.Contains(date.Date);
                CreateDayCell(date, isCompleted);
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(calendarGrid as RectTransform);
    }

    // ─────────────────────────────────────────
    //  CELL CREATION
    // ─────────────────────────────────────────

    private void CreateDayCell(DateTime date, bool isCompleted)
    {
        GameObject obj = BuildCellFromCode(date, isCompleted);

        // Pritaikome šriftą dienos skaičiui
        TextMeshProUGUI dayText = obj.GetComponentInChildren<TextMeshProUGUI>();
        ApplyRussoToText(dayText);

        CalendarDay script = obj.GetComponent<CalendarDay>();
        if (script == null) script = obj.AddComponent<CalendarDay>();

        script.Initialize(date, isCompleted);
        script.OnDayClicked += OnDayCellClicked;
    }

    private GameObject BuildCellFromCode(DateTime date, bool isCompleted)
    {
        GameObject root = new GameObject($"Day_{date.Day:00}");
        root.transform.SetParent(calendarGrid, false);
        root.SetActive(true);

        RectTransform rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(150, 120);
        rt.localScale = Vector3.one;

        Image bg = root.AddComponent<Image>();
        bg.raycastTarget = true;

        if (date.Date == DateTime.Today)
            bg.color = ColorMint;
        else if (isCompleted)
            bg.color = new Color(ColorTeal.r, ColorTeal.g, ColorTeal.b, 0.4f);
        else if (date.Date > DateTime.Today)
            bg.color = new Color(0.85f, 0.85f, 0.85f, 0.25f);
        else
            bg.color = new Color(ColorCoral.r, ColorCoral.g, ColorCoral.b, 0.25f);

        Button btn = root.AddComponent<Button>();
        btn.targetGraphic = bg;

        // Dienos skaičius
        GameObject textObj = new GameObject("DayText");
        textObj.transform.SetParent(root.transform, false);
        textObj.SetActive(true);

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = date.Day.ToString();
        tmp.fontSize = 40;  // Dydis 40
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;
        tmp.raycastTarget = false;

        // PRIVERSTINIS FONTO NUSTATYMAS
        if (russoFont != null)
        {
            tmp.font = russoFont;
            Debug.Log($"✅ Font set for day {date.Day}: {russoFont.name}");
        }
        else
        {
            Debug.LogWarning($"⚠️ russoFont is null for day {date.Day}");
            // Pabandom dar kartą užkrauti fontą
            LoadRussoFont();
            if (russoFont != null) tmp.font = russoFont;
        }

        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 0.15f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        // Completion taškas
        if (isCompleted)
        {
            GameObject dot = new GameObject("Dot");
            dot.transform.SetParent(root.transform, false);
            dot.SetActive(true);

            Image dotImg = dot.AddComponent<Image>();
            dotImg.color = ColorTeal;
            dotImg.raycastTarget = false;

            RectTransform dr = dot.GetComponent<RectTransform>();
            dr.sizeDelta = new Vector2(12, 12);
            dr.anchorMin = new Vector2(0.5f, 0f);
            dr.anchorMax = new Vector2(0.5f, 0f);
            dr.pivot = new Vector2(0.5f, 0f);
            dr.anchoredPosition = new Vector2(0f, 14f);
        }

        return root;
    }

    private void CreateEmptyCell()
    {
        GameObject obj = new GameObject("Empty");
        obj.transform.SetParent(calendarGrid, false);
        obj.SetActive(true);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80, 80);
        rt.localScale = Vector3.one;

        Image img = obj.AddComponent<Image>();
        img.color = Color.clear;
        img.raycastTarget = false;
    }

    // ─────────────────────────────────────────
    //  SUPABASE
    // ─────────────────────────────────────────

    private async Task InitializeSupabaseAndLoadData()
    {
        int attempts = 0;
        while (SupabaseManager.Instance == null)
        {
            await Task.Delay(200);
            if (++attempts >= 50) { Debug.LogError("❌ SupabaseManager not found"); return; }
        }

        if (SupabaseManager.Instance.Auth?.CurrentUser == null)
            await TryLogin();

        attempts = 0;
        while (SupabaseManager.Instance.Auth?.CurrentUser == null)
        {
            await Task.Delay(200);
            if (++attempts >= 50) { Debug.LogError("❌ No user logged in"); return; }
        }

        Debug.Log($"✅ Logged in: {SupabaseManager.Instance.Auth.CurrentUser.Email}");
        isSupabaseReady = true;

        await LoadCompletedDays();
        GenerateCalendar();
    }

    private async Task TryLogin()
    {
        try
        {
            await SupabaseManager.Instance.Auth.SignIn(testEmail, testPassword);
        }
        catch
        {
            try
            {
                await SupabaseManager.Instance.Auth.SignUp(testEmail, testPassword);
                await SupabaseManager.Instance.Auth.SignIn(testEmail, testPassword);
            }
            catch (Exception e) { Debug.LogError($"❌ Login failed: {e.Message}"); }
        }
    }

    private async Task LoadCompletedDays()
    {
        completedDays.Clear();

        try
        {
            var habits = await habitController.GetAllHabitsAsync();
            if (habits != null)
                foreach (var habit in habits)
                {
                    if (habit.CompletionDataList == null) continue;
                    for (int i = 0; i < habit.CompletionDataList.Count; i++)
                        if (habit.CompletionDataList[i])
                            completedDays.Add(habit.DateOfCreation.AddDays(i).Date);
                }
        }
        catch (Exception e) { Debug.LogError($"Habits error: {e.Message}"); }

        try
        {
            var challenges = await userChallengeController.GetAllUserChallengesAsync();
            if (challenges != null)
                foreach (var c in challenges)
                    if (c.CompletedDate.HasValue)
                        completedDays.Add(c.CompletedDate.Value.Date);
        }
        catch (Exception e) { Debug.LogError($"Challenges error: {e.Message}"); }

        Debug.Log($"📅 Completed days: {completedDays.Count}");
    }

    // ─────────────────────────────────────────
    //  DETAIL PANEL
    // ─────────────────────────────────────────

    private async void OnDayCellClicked(DateTime date, bool isCompleted)
    {
        if (dateTitle != null)
            dateTitle.text = date.ToString("yyyy-MM-dd, dddd");

        await ShowHabitsForDate(date);

        if (detailPanel != null)
            detailPanel.SetActive(true);
    }

    private async Task ShowHabitsForDate(DateTime date)
    {
        if (!isSupabaseReady) return;

        if (itemListContent != null)
            foreach (Transform child in itemListContent)
                Destroy(child.gameObject);

        List<Habit> habits = null;
        try { habits = await habitController.GetAllHabitsAsync(); }
        catch (Exception e) { Debug.LogError($"Error: {e.Message}"); return; }
        if (habits == null) return;

        var done = new List<Habit>();
        foreach (var habit in habits)
        {
            if (habit.CompletionDataList == null) continue;
            int idx = (date.Date - habit.DateOfCreation.Date).Days;
            if (idx >= 0 && idx < habit.CompletionDataList.Count && habit.CompletionDataList[idx])
                done.Add(habit);
        }

        if (emptyText != null) emptyText.SetActive(done.Count == 0);

        if (itemListContent != null)
            foreach (var habit in done)
            {
                GameObject item = new GameObject("HabitItem_" + habit.Title);
                item.transform.SetParent(itemListContent, false);

                TextMeshProUGUI tmp = item.AddComponent<TextMeshProUGUI>();
                tmp.text = $"✅  {habit.Title}";
                tmp.fontSize = 18;
                tmp.color = ColorTeal;
                tmp.raycastTarget = false;

                // PRITAIKOME RUSSO ONE ŠRIFTĄ
                if (russoFont != null)
                    tmp.font = russoFont;

                item.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 35);
            }
    }

    public void CloseDetailPanel()
    {
        if (detailPanel != null) detailPanel.SetActive(false);

        // Papildomas patikrinimas closeButton
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners(); // Išvalom senus
            closeButton.onClick.AddListener(() => {
                if (detailPanel != null)
                    detailPanel.SetActive(false);
                Debug.Log("Detail panel closed");
            });
        }
    }

    // ─────────────────────────────────────────
    //  NAVIGATION
    // ─────────────────────────────────────────

    public async void NextMonth()
    {
        currentMonth++;
        if (currentMonth > 12) { currentMonth = 1; currentYear++; }
        if (isSupabaseReady) await LoadCompletedDays();
        GenerateCalendar();
    }

    public async void PreviousMonth()
    {
        currentMonth--;
        if (currentMonth < 1) { currentMonth = 12; currentYear--; }
        if (isSupabaseReady) await LoadCompletedDays();
        GenerateCalendar();
    }

    private void UpdateMonthYearText()
    {
        if (monthYearText == null) return;
        string[] months = { "", "Sausis", "Vasaris", "Kovas", "Balandis", "Gegužė",
                             "Birželis", "Liepa", "Rugpjūtis", "Rugsėjis", "Spalis",
                             "Lapkritis", "Gruodis" };
        monthYearText.text = $"{months[currentMonth]} {currentYear}";
    }

    // ─────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────

    private static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }
}