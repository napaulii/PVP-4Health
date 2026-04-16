using SupabaseModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class HabitManager : MonoBehaviour
{
    private HabitController _habitController;
    [SerializeField] private string testEmail = "testuser@gmail.com";
    [SerializeField] private string testPassword = "password123";
    [Header("List UI References")]
    public Transform listContentContainer; // The "Content" object of a Scroll View
    public GameObject habitItemPrefab;     // The Button Prefab with HabitListItemUI attached[Header("Details View UI References")]
    public GameObject detailsPanel;        // The popup window for the specific habit
    public TextMeshProUGUI detailsTitleText;
    public TextMeshProUGUI detailsDescText;

    private long _currentlySelectedHabitId; // Remembers which habit we are currently looking at

    async void Start()
    {
        await SupabaseManager.Instance.Auth.SignIn(testEmail, testPassword);
        _habitController = new HabitController();
        detailsPanel.SetActive(false); // Hide details panel at start
        FetchAllHabits();
    }
    // 1. FETCH ALL AND SPAWN UI
    public async void FetchAllHabits()
    {
        Debug.Log("Fetching all habits...");
        List<Habit> allHabits = await _habitController.GetAllHabitsAsync();

        // Clear out the old UI list before spawning new ones
        foreach (Transform child in listContentContainer)
        {
            Destroy(child.gameObject);
        }

        if (allHabits != null)
        {
            foreach (var habit in allHabits)
            {
                // Spawn a new button prefab
                GameObject newObj = Instantiate(habitItemPrefab, listContentContainer);

                // Get the script and pass the data to it
                HabitListItemUI uiScript = newObj.GetComponent<HabitListItemUI>();
                uiScript.Setup(habit, this);
            }
        }
    }

    // 2. OPEN SPECIFIC HABIT (Called by HabitListItemUI)
    public async void OpenHabitDetails(long habitId)
    {
        _currentlySelectedHabitId = habitId; // Save this ID so the Edit/Delete buttons know what to target

        Debug.Log($"Fetching details for habit {habitId}...");
        Habit habit = await _habitController.GetHabitByIdAsync(habitId);

        if (habit != null)
        {
            // Update the UI texts
            detailsTitleText.text = habit.Title;
            detailsDescText.text = habit.Description;

            // Show the panel
            detailsPanel.SetActive(true);
        }
    }

    // 3. ACTIONS ON SELECTED HABIT
    public void DeleteSelectedHabit()
    {
        // Now you don't need parameters from the Unity Button OnClick event!
        // It uses the ID we saved when the window was opened.
        DeleteHabit(_currentlySelectedHabitId);
    }

    private async void DeleteHabit(long habitId)
    {
        bool isDeleted = await _habitController.DeleteHabitAsync(habitId);
        if (isDeleted)
        {
            detailsPanel.SetActive(false); // Close the window
            FetchAllHabits(); // Refresh the list
        }
    }
}