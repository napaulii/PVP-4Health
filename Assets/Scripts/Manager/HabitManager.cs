using SupabaseModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class HabitManager : MonoBehaviour
{
    private HabitController _habitController;
    private UserController _userController;
    [SerializeField] private string testEmail = "testuser@game.com";
    [SerializeField] private string testPassword = "password123";

    [Header("List UI References")]
    public Transform listContentContainer;
    public GameObject habitItemPrefab;

    [Header("Details View UI References")]
    public GameObject detailsPanel;
    public GameObject dimBackground;
    public TextMeshProUGUI detailsTitleText;
    public TextMeshProUGUI detailsDescText;
    public TextMeshProUGUI detailsStreakText;
    public TextMeshProUGUI detailsLongestStreakText;
    public TextMeshProUGUI detailsDateText;

    [Header("Create UI References")]
    public GameObject createPanel;
    public TMP_InputField createTitleInput;
    public TMP_InputField createDescInput;

    [Header("Edit UI References")]
    public GameObject editPanel;
    public TMP_InputField editTitleInput;
    public TMP_InputField editDescInput;

    [Header("Scroll References")]
    public ScrollRect habitScrollRect;

    private long _currentlySelectedHabitId;

    async void Start()
    {
        detailsPanel.SetActive(false);
        editPanel.SetActive(false);
        createPanel.SetActive(false);
        if (dimBackground != null) dimBackground.SetActive(false);
        SetScrollingEnabled(true);

        await SupabaseManager.Instance.Auth.SignIn(testEmail, testPassword);
        Debug.Log($"Logged in {SupabaseManager.Instance.Auth.CurrentUser.Id}");

        _habitController = new HabitController();
        _userController = new UserController();
        FetchAllHabits();
    }

    private void SetScrollingEnabled(bool isEnabled)
    {
        if (habitScrollRect != null)
        {
            habitScrollRect.enabled = isEnabled;
            if (!isEnabled) habitScrollRect.velocity = Vector2.zero;
        }
    }

    public async void FetchAllHabits()
    {
        Debug.Log("Fetching all habits...");
        List<Habit> allHabits = await _habitController.GetAllHabitsAsync();

        while (listContentContainer.childCount > 0)
        {
            Transform child = listContentContainer.GetChild(0);
            child.SetParent(null);
            Destroy(child.gameObject);
        }

        if (allHabits != null)
        {
            allHabits = allHabits.OrderByDescending(h => h.Id).ToList();

            foreach (var habit in allHabits)
            {
                GameObject newObj = Instantiate(habitItemPrefab, listContentContainer);
                newObj.transform.localScale = Vector3.one;
                HabitListItemUI uiScript = newObj.GetComponent<HabitListItemUI>();
                uiScript.Setup(habit, this);
            }
        }
        Canvas.ForceUpdateCanvases();
    }

    public void OpenCreatePanel()
    {
        Debug.Log("Opening create panel");
        createTitleInput.text = "";
        createDescInput.text = "";

        detailsPanel.SetActive(false);
        editPanel.SetActive(false);

        createPanel.SetActive(true);
        if (dimBackground != null) dimBackground.SetActive(true);
        SetScrollingEnabled(false);
    }

    public async void CreateNewHabit()
    {
        if (string.IsNullOrEmpty(createTitleInput.text)) return;

        Debug.Log("Creating new habit...");
        Habit newHabit = await _habitController.CreateHabitAsync(createTitleInput.text, createDescInput.text);

        if (newHabit != null)
        {
            CloseCreate();
            FetchAllHabits();
        }
    }

    public void CloseCreate()
    {
        Debug.Log("Closing create panel");
        createPanel.SetActive(false);
        if (dimBackground != null) dimBackground.SetActive(false);
        SetScrollingEnabled(true);
    }

    public async void OpenHabitDetails(long habitId)
    {
        Debug.Log("Opening details panel");
        _currentlySelectedHabitId = habitId;

        SetScrollingEnabled(false);
        if (dimBackground != null) dimBackground.SetActive(true);
        createPanel.SetActive(false);

        Habit habit = await _habitController.GetHabitByIdAsync(habitId);

        if (habit != null)
        {
            detailsTitleText.text = habit.Title;
            detailsDescText.text = habit.Description;
            detailsStreakText.text = $"Streak: {habit.CurrentStreak}";
            detailsLongestStreakText.text = $"Longest Streak: {habit.LongestStreak}";
            detailsDateText.text = $"Created on: {habit.DateOfCreation.ToString("yyyy-MM-dd")}";
            detailsPanel.SetActive(true);
        }
    }

    public void CloseDetails()
    {
        Debug.Log("Closing details panel");
        detailsPanel.SetActive(false);
        if (dimBackground != null) dimBackground.SetActive(false);
        SetScrollingEnabled(true);
    }

    public void OpenEditPanel()
    {
        Debug.Log("Opening edit panel");
        detailsPanel.SetActive(false);
        editTitleInput.text = detailsTitleText.text;
        editDescInput.text = detailsDescText.text;

        editPanel.SetActive(true);
        if (dimBackground != null) dimBackground.SetActive(true);
        SetScrollingEnabled(false);
    }

    public async void SaveEdit()
    {
        Debug.Log("Saving edits");
        await _habitController.UpdateHabitAsync(_currentlySelectedHabitId, editTitleInput.text, editDescInput.text);
        editPanel.SetActive(false);
        OpenHabitDetails(_currentlySelectedHabitId);
        FetchAllHabits();
    }

    public void CloseEdits()
    {
        Debug.Log("Closing edits");
        editPanel.SetActive(false);
        detailsPanel.SetActive(true);
    }

    public async void DeleteSelectedHabit()
    {
        Debug.Log("Deleting selected Habit");
        bool isDeleted = await _habitController.DeleteHabitAsync(_currentlySelectedHabitId);
        if (isDeleted)
        {
            detailsPanel.SetActive(false);
            if (dimBackground != null) dimBackground.SetActive(false);
            SetScrollingEnabled(true);
            FetchAllHabits();
        }
    }

    public async void ToggleHabitCompletion(long habitID)
    {
        Debug.Log("Marking as completed");
        await _habitController.ToggleCompletionAsync(habitID);
        await _userController.UpdateUserAsync(10, 10, true);
        FetchAllHabits();
    }
}