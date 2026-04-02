using UnityEngine;
using System.Threading.Tasks;
using SupabaseModels; // Ensure this matches your namespace
using System;

public class HabitTester : MonoBehaviour
{
    private HabitController _habitController;

    [Header("Test Habit Data")]
    [SerializeField] private string inputTitle = "Drink Water";
    [SerializeField] private string inputDescription = "Drink 2 liters today";

    [Header("Test User Auth")]
    [SerializeField] private string testEmail = "testuser@gmail.com";
    [SerializeField] private string testPassword = "password123";

    async void Start()
    {
        _habitController = new HabitController();

        // 1. Sign up or Login the test user
        await EnsureUserIsReady();
    }

    private async Task EnsureUserIsReady()
    {
        try
        {
            Debug.Log("Attempting to Sign Up test user...");

            // Try to sign up
            var session = await SupabaseManager.Instance.Auth.SignUp(testEmail, testPassword);

            if (session != null && session.User != null)
            {
                Debug.Log("New user created in Auth! Now creating Game Profile...");

                // IMPORTANT: Since you added the Foreign Key, we MUST create the profile row 
                // in your 'public.User' table before we can add Habits for them.
                var profile = new SupabaseModels.User
                {
                    Id = session.User.Id,
                    Email = testEmail,
                    Nickname = "TestHero",
                    Balance = 0,
                    Xp = 0,
                    FirstName = "TEST",
                    LastName = "TEST"
                };

                await SupabaseManager.Instance.From<SupabaseModels.User>().Insert(profile);
                Debug.Log("Game Profile created successfully!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"badbadbad: {e.Message}");
            // If SignUp fails, it's usually because the user already exists.
            // So we try to Login instead.
            Debug.Log("User likely already exists. Attempting Login...");
            try
            {
                await SupabaseManager.Instance.Auth.SignIn(testEmail, testPassword);
                Debug.Log($"User logged in successfully! ID: {SupabaseManager.Instance.Auth.CurrentUser.Id}");
            }
            catch (Exception loginErr)
            {
                Debug.LogError($"Login failed: {loginErr.Message}");
            }
        }
    }

    // 2. This is linked to your Unity Button
    public async void OnTestButtonClicked()
    {
        // Safety check to ensure we are logged in
        if (SupabaseManager.Instance.Auth.CurrentUser == null)
        {
            Debug.LogError("Cannot create habit: No user logged in.");
            return;
        }

        Debug.Log("Button Clicked! Creating habit...");

        // Note: Make sure your HabitController matches the column names in your DB screenshot!
        Habit newHabit = await _habitController.CreateHabitAsync(inputTitle, inputDescription);

        if (newHabit != null)
        {
            Debug.Log($"<color=green>SUCCESS!</color> Habit '{newHabit.Title}' saved to Supabase with ID: {newHabit.Id}");
        }
        else
        {
            Debug.Log("<color=red>FAILED</color> to save habit. Check the console for errors.");
        }
    }
}