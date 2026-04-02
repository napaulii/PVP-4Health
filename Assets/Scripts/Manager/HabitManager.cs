using UnityEngine;
using SupabaseModels;

public class HabitUIManager : MonoBehaviour
{
    private HabitController _habitController;

    // These represent your input fields (using string variables as requested)
    public string inputFieldTitle = "Run 5km";
    public string inputFieldDescription = "Morning run in the park";

    void Start()
    {
        // Initialize the controller
        _habitController = new HabitController();
    }

    // Call this method from your Unity "Submit" Button OnClick event
    public async void SubmitNewHabit()
    {
        // Basic validation before sending to the controller
        if (string.IsNullOrEmpty(inputFieldTitle))
        {
            Debug.LogWarning("Title cannot be empty!");
            return;
        }

        Debug.Log("Attempting to create habit...");

        // Pass the string variables to the controller
        Habit createdHabit = await _habitController.CreateHabitAsync(
            inputFieldTitle,
            inputFieldDescription
        );

        if (createdHabit != null)
        {
            Debug.Log($"Successfully saved to database! Habit ID: {createdHabit.Id}");

            // Clear your input strings/fields here so the user can type a new one
            inputFieldTitle = "";
            inputFieldDescription = "";
        }
        else
        {
            Debug.LogError("Failed to create habit in database.");
        }
    }
}