using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;
using SupabaseModels; // Ensure this matches your namespace for the User model

public class LoginManager : MonoBehaviour
{
    [Header("Login UI")]
    [SerializeField] private TMP_InputField loginEmailField;
    [SerializeField] private TMP_InputField loginPasswordField;
    [SerializeField] private Button loginButton;

    [Header("Registration UI")]
    [SerializeField] private TMP_InputField regEmailField;
    [SerializeField] private TMP_InputField regPasswordField;
    [SerializeField] private Button registerButton;

    [Header("General UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject loginPanel;   // The whole Login/Register screen
    [SerializeField] private GameObject SetupPanel;   // The screen to show after success

    private void Start()
    {
        if (statusText != null) statusText.text = "Welcome!";
        
        // Setup Button Listeners
        loginButton.onClick.AddListener(OnLoginButtonClicked);
        registerButton.onClick.AddListener(OnRegisterButtonClicked);

        // Ensure UI state
        if (loginPanel != null) loginPanel.SetActive(true);
        if (SetupPanel != null) SetupPanel.SetActive(false);
    }

    private async void OnLoginButtonClicked()
    {
        string email = loginEmailField.text.Trim();
        string password = loginPasswordField.text;

        if (!ValidateInputs(email, password)) return;

        SetLoadingState(true);
        UpdateStatus("Logging in...", Color.white);

        try
        {
            var session = await SupabaseManager.Instance.Auth.SignIn(email, password);
            if (session?.User != null)
            {
                CompleteAccess();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Login Error: {ex.Message}");
            UpdateStatus($"Login Failed: {ex.Message}", Color.red);
            SetLoadingState(false);
        }
    }

    public async void OnRegisterButtonClicked()
    {
        string email = regEmailField.text.Trim();
        string password = regPasswordField.text;

        if (!ValidateInputs(email, password)) return;

        SetLoadingState(true);
        UpdateStatus("Creating account...", Color.white);

        try
        {
            // 1. Sign Up the user in Supabase Auth
            var session = await SupabaseManager.Instance.Auth.SignUp(email, password);

            if (session?.User != null)
            {
                UpdateStatus("Auth created! Setting up profile...", Color.cyan);

                // 2. Create the Game Profile row in the 'public.User' table
                // Using SupabaseModels.User to avoid conflicts with other 'User' classes
                var newProfile = new SupabaseModels.User
                {
                    Id = session.User.Id,
                    Email = email,
                    Nickname = "NewHero", // You could add another input field for this!
                    Balance = 0,
                    Xp = 0,
                    FirstName = "New",
                    LastName = "User"
                };

                await SupabaseManager.Instance.From<SupabaseModels.User>().Insert(newProfile);
                
                UpdateStatus("Registration Successful!", Color.green);
                CompleteAccess();
            }
        }
        catch (Exception e)
        {
            // Fallback logic: If user exists, try to log them in instead
            Debug.LogWarning($"Sign up failed, attempting login: {e.Message}");
            try
            {
                await SupabaseManager.Instance.Auth.SignIn(email, password);
                CompleteAccess();
            }
            catch (Exception loginErr)
            {
                UpdateStatus($"Error: {loginErr.Message}", Color.red);
                SetLoadingState(false);
            }
        }
    }

    private void CompleteAccess()
    {
        UpdateStatus("Access Granted!", Color.green);
        
        // Hide login UI and show the main app
        if (loginPanel != null) loginPanel.SetActive(false);
        if (SetupPanel != null) SetupPanel.SetActive(true);
        
        SetLoadingState(false);
    }

    private bool ValidateInputs(string email, string pass)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            UpdateStatus("Fields cannot be empty.", Color.yellow);
            return false;
        }
        if (pass.Length < 6)
        {
            UpdateStatus("Password too short (min 6).", Color.yellow);
            return false;
        }
        return true;
    }

    private void UpdateStatus(string message, Color color)
    {
        if (statusText == null) return;
        statusText.text = message;
        statusText.color = color;
    }

    private void SetLoadingState(bool isLoading)
    {
        loginButton.interactable = !isLoading;
        registerButton.interactable = !isLoading;
        
        // Optional: Toggle a loading spinner here
    }
}