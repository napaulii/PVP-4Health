using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SupabaseModels; 

// Resolve Ambiguities
using User = SupabaseModels.User; 
using Group = SupabaseModels.Group;
using Fortress = SupabaseModels.Fortress;
using UserChallenge = SupabaseModels.UserChallenge;

public class LoginManager : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string mainGameSceneName = "MainGameScene";

    [Header("Authentication UI")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private TMP_InputField emailField;
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private Button loginButton;
    [SerializeField] private Button registerButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Survey (Onboarding)")]
    [SerializeField] private GameObject surveyPanel;
    [SerializeField] private GameObject[] surveyPages;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button finishSurveyButton;
    
    [Header("Page 1: Profile")]
    [SerializeField] private TMP_InputField firstNameInput;
    [SerializeField] private TMP_InputField lastNameInput;
    [SerializeField] private TMP_InputField nicknameInput;

    [Header("Page 2: Path")]
    [SerializeField] private TMP_Dropdown pathDropdown;

    [Header("Page 3: Fortress (Multiplayer)")]
    [SerializeField] private Toggle joinExistingToggle; 
    [SerializeField] private TMP_InputField joinGroupIdInput; // Input for joining
    [SerializeField] private TMP_InputField newGroupNameInput; // Input for creating

    private int currentSurveyPage = 0;

    private void Start()
    {
        loginButton.onClick.AddListener(() => OnAuthClicked(false));
        registerButton.onClick.AddListener(() => OnAuthClicked(true));
        nextButton.onClick.AddListener(NextSurveyPage);
        finishSurveyButton.onClick.AddListener(OnFinishSurveyClicked);

        loginPanel.SetActive(true);
        surveyPanel.SetActive(false);
    }

    private async void OnAuthClicked(bool isRegistering)
    {
        string email = emailField.text.Trim();
        string pass = passwordField.text;

        if (string.IsNullOrEmpty(email) || pass.Length < 6)
        {
            UpdateStatus("Invalid credentials.", Color.yellow);
            return;
        }

        SetLoadingState(true);
        try
        {
            if (isRegistering) await SupabaseManager.Instance.Auth.SignUp(email, pass);
            else await SupabaseManager.Instance.Auth.SignIn(email, pass);

            await DirectUserFlow();
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error: {ex.Message}", Color.red);
            SetLoadingState(false);
        }
    }

    private async Task DirectUserFlow()
    {
        string userId = SupabaseManager.Instance.Auth.CurrentUser.Id;
        var userResponse = await SupabaseManager.Instance.From<User>().Where(u => u.Id == userId).Get();

        loginPanel.SetActive(false);
        if (userResponse.Models.Count == 0) StartSurvey();
        else EnterMainGame();
    }

    private void StartSurvey()
    {
        surveyPanel.SetActive(true);
        currentSurveyPage = 0;
        UpdateSurveyPageVisibility();
    }

    private void NextSurveyPage()
    {
        currentSurveyPage++;
        UpdateSurveyPageVisibility();
    }

    private void UpdateSurveyPageVisibility()
    {
        for (int i = 0; i < surveyPages.Length; i++)
            surveyPages[i].SetActive(i == currentSurveyPage);
        
        nextButton.gameObject.SetActive(currentSurveyPage < surveyPages.Length - 1);
        finishSurveyButton.gameObject.SetActive(currentSurveyPage == surveyPages.Length - 1);
    }

private async void OnFinishSurveyClicked()
{
    SetLoadingState(true);
    try
    {
        var currentUser = SupabaseManager.Instance.Auth.CurrentUser;
        if (currentUser == null) throw new Exception("No authenticated user found.");

        string userId = currentUser.Id;
        
        // 1. Create the User Profile FIRST
        // We leave GroupID null for now to avoid the Foreign Key crash
        var newProfile = new User {
            Id = userId, 
            Email = currentUser.Email,
            FirstName = firstNameInput.text, 
            LastName = lastNameInput.text,
            Nickname = nicknameInput.text,
            Balance = 0,
            Xp = 0
        };

        Debug.Log($"Attempting to insert user with ID: {userId}");
        if (string.IsNullOrEmpty(userId)) {
            Debug.LogError("UserId is NULL before insertion!");
            return;
        }

        // CRITICAL: Ensure we use the Return.Representation to confirm the insert worked
        await SupabaseManager.Instance.From<User>().Insert(newProfile);

        // 2. Now handle the Group creation
        long assignedGroupId;
        if (joinExistingToggle.isOn)
        {
            assignedGroupId = await JoinSpecifiedGroup(long.Parse(joinGroupIdInput.text));
        }
        else
        {
            assignedGroupId = await CreateNewGroup(userId, newGroupNameInput.text);
        }

        // 3. Update the User with the Group ID
        newProfile.GroupID = assignedGroupId;
        await SupabaseManager.Instance.From<User>().Update(newProfile);

        await CreateInitialChallenge(userId);
        EnterMainGame();
    }
    catch (Exception ex)
    {
        Debug.LogError($"Registration Failed: {ex.Message}");
        UpdateStatus("Error during setup. Check console.", Color.red);
        SetLoadingState(false);
    }
}

    private async Task<long> CreateNewGroup(string userId, string title)
    {
        if (string.IsNullOrEmpty(title)) title = "The New Bastion";

        var newGroup = new Group { Title = title, UserId = userId };
        var groupRes = await SupabaseManager.Instance
    .From<Group>()
    .Insert(newGroup, new Postgrest.QueryOptions { Returning = Postgrest.QueryOptions.ReturnType.Representation });
        
        if (groupRes.Models.Count == 0) throw new Exception("Failed to create group.");
        
        long groupId = groupRes.Models[0].Id;

        var newFortress = new Fortress { State = "Stable", Level = 1, GroupId = groupId };
        await SupabaseManager.Instance.From<Fortress>().Insert(newFortress);

        return groupId;
    }

    private async Task<long> JoinSpecifiedGroup(long groupId)
    {
        // Check if group exists
        var groupRes = await SupabaseManager.Instance.From<Group>().Where(g => g.Id == groupId).Get();
        if (groupRes.Models.Count == 0) throw new Exception("Group ID not found.");

        // Check if group is full (max 4)
        var userCountRes = await SupabaseManager.Instance.From<User>().Where(u => u.GroupID == groupId).Get();
        if (userCountRes.Models.Count >= 4) throw new Exception("This Fortress is full (4/4).");

        return groupId;
    }

    private async Task CreateInitialChallenge(string userId)
    {
        var firstChallenge = new UserChallenge {
            Status = "Active", Date = DateTime.UtcNow,
            TimeToComplete = 1440, UserId = userId,
            ChallengeId = pathDropdown.value + 1 
        };
        await SupabaseManager.Instance.From<UserChallenge>().Insert(firstChallenge);
    }

    private void EnterMainGame() => SceneManager.LoadScene(mainGameSceneName);

    private void UpdateStatus(string message, Color color)
    {
        if (statusText) { statusText.text = message; statusText.color = color; }
    }

    private void SetLoadingState(bool isLoading)
    {
        loginButton.interactable = !isLoading;
        registerButton.interactable = !isLoading;
        if (nextButton) nextButton.interactable = !isLoading;
        if (finishSurveyButton) finishSurveyButton.interactable = !isLoading;
    }
}