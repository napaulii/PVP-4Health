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
        Debug.Log("LoginManager: Starting initialization.");

        loginButton.onClick.AddListener(() => OnAuthClicked(false));
        registerButton.onClick.AddListener(() => OnAuthClicked(true));
        nextButton.onClick.AddListener(NextSurveyPage);
        finishSurveyButton.onClick.AddListener(OnFinishSurveyClicked);

        loginPanel.SetActive(true);
        surveyPanel.SetActive(false);
        
        Debug.Log("LoginManager: Initializing UI state.");
        SetLoadingState(false);
    }

    private async void OnAuthClicked(bool isRegistering)
    {
        string email = emailField.text.Trim();
        string pass = passwordField.text;
        
        Debug.Log($"LoginManager: Auth clicked. Is Registering: {isRegistering}. Email: {email}");

        if (string.IsNullOrEmpty(email) || pass.Length < 6)
        {
            UpdateStatus("Invalid credentials.", Color.yellow);
            Debug.LogWarning("LoginManager: Authentication failed due to empty/short input.");
            return;
        }

        SetLoadingState(true);
        try
        {
            if (isRegistering) 
            {
                Debug.Log($"LoginManager: Attempting Sign Up for {email}.");
                await SupabaseManager.Instance.Auth.SignUp(email, pass);
                Debug.Log("LoginManager: Sign Up successful.");
            }
            else 
            {
                Debug.Log($"LoginManager: Attempting Sign In for {email}.");
                await SupabaseManager.Instance.Auth.SignIn(email, pass);
                Debug.Log("LoginManager: Sign In successful.");
            }

            await DirectUserFlow();
        }
        catch (Exception ex)
        {
            UpdateStatus($"Error: {ex.Message}", Color.red);
            Debug.LogError($"LoginManager: Authentication Error caught: {ex}");
            SetLoadingState(false);
        }
    }

    private async Task DirectUserFlow()
    {
        // Assuming SupabaseManager.Instance.Auth.CurrentUser is populated after successful auth
        var currentUser = SupabaseManager.Instance.Auth.CurrentUser;
        if (currentUser == null) 
        {
            Debug.LogError("LoginManager: Current user object is NULL in DirectUserFlow.");
            UpdateStatus("Attempting login...", Color.green);
            SetLoadingState(false);
            OnAuthClicked(false);
            return;
        }

        string userId = currentUser.Id;
        Debug.Log($"LoginManager: User ID retrieved for flow control: {userId}");


        // Fetch user from DB to check if they completed onboarding
        try
        {
            Debug.Log($"LoginManager: Querying database for existing user profile with ID: {userId}");
            var userResponse = await SupabaseManager.Instance.From<User>().Where(u => u.Id == userId).Get();

            loginPanel.SetActive(false);
            
            if (userResponse.Models.Count == 0) 
            {
                Debug.Log("LoginManager: User not found in DB. Starting Survey.");
                StartSurvey();
            }
            else 
            {
                Debug.Log("LoginManager: User profile found. Proceeding to Main Game.");
                EnterMainGame();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"LoginManager: Error during user flow check: {ex}");
            UpdateStatus("Failed to retrieve user data after login.", Color.red);
            SetLoadingState(false);
        }
    }

    private void StartSurvey()
    {
        currentSurveyPage = 0;
        surveyPanel.SetActive(true);
        Debug.Log("LoginManager: Starting Survey/Onboarding flow.");
        UpdateSurveyPageVisibility();
    }

    private void NextSurveyPage()
    {
        currentSurveyPage++;
        Debug.Log($"LoginManager: Moving to survey page {currentSurveyPage}.");
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
    Debug.Log("LoginManager: Finishing Survey and proceeding to user setup.");
    SetLoadingState(true);
    try
    {
        var currentUser = SupabaseManager.Instance.Auth.CurrentUser;
        if (currentUser == null) throw new Exception("No authenticated user found during survey completion.");

        string userId = currentUser.Id;
        Debug.Log($"LoginManager: User ID for setup: {userId}");
        
        // 1. Create the User Profile FIRST
        var newProfile = new User {
            Id = userId, 
            Email = currentUser.Email,
            FirstName = firstNameInput.text, 
            LastName = lastNameInput.text,
            Nickname = nicknameInput.text,
            Balance = 0,
            Xp = 0
        };

        Debug.Log($"LoginManager: Preparing to insert new User Profile for {userId}.");
        if (string.IsNullOrEmpty(userId)) {
            Debug.LogError("LoginManager: UserId is NULL before insertion!");
            return;
        }

        // CRITICAL: Ensure we use the Return.Representation to confirm the insert worked
        try
        {
             await SupabaseManager.Instance.From<User>().Insert(newProfile);
             Debug.Log($"LoginManager: Successfully inserted new User record for {userId}.");
        }
        catch (Exception e)
        {
            Debug.LogError($"LoginManager: FAILED TO INSERT USER PROFILE: {e.Message}");
            throw; // Re-throw to hit the main catch block
        }


        // 2. Now handle the Group creation/joining
        long assignedGroupId;
        if (joinExistingToggle.isOn)
        {
            Debug.Log("LoginManager: Joining existing group...");
            assignedGroupId = await JoinSpecifiedGroup(long.Parse(joinGroupIdInput.text));
        }
        else
        {
            Debug.Log($"LoginManager: Creating new group with name: {newGroupNameInput.text}");
            assignedGroupId = await CreateNewGroup(userId, newGroupNameInput.text);
        }

        // 3. Update the User with the Group ID
        newProfile.GroupID = assignedGroupId;
        Debug.Log($"LoginManager: Preparing to update user {userId} with GroupID: {assignedGroupId}.");
        try
        {
            await SupabaseManager.Instance.From<User>().Update(newProfile);
            Debug.Log($"LoginManager: Successfully updated User record with Group ID.");
        }
        catch (Exception e)
        {
            Debug.LogError($"LoginManager: FAILED TO UPDATE USER WITH GROUPID: {e.Message}");
            throw; // Re-throw to hit the main catch block
        }

        EnterMainGame();
    }
    catch (Exception ex)
    {
        Debug.LogError($"LoginManager: Registration/Setup Flow Failed: {ex}");
        UpdateStatus("Error during setup. Check console for details.", Color.red);
        SetLoadingState(false);
    }
}

    private async Task<long> CreateNewGroup(string userId, string title)
    {
        if (string.IsNullOrEmpty(title)) { 
            Debug.LogWarning("LoginManager: Group title was empty, defaulting to 'The New Bastion'.");
            title = "The New Bastion";
        }

        var newGroup = new Group { Title = title, AdminId = userId };
        Debug.Log($"LoginManager: Inserting new Group '{title}' for user {userId}.");
        try
        {
             // Use QueryOptions to get the inserted ID back
            var groupRes = await SupabaseManager.Instance
                .From<Group>()
                .Insert(newGroup, new Postgrest.QueryOptions { Returning = Postgrest.QueryOptions.ReturnType.Representation });
            
            if (groupRes.Models.Count == 0) throw new Exception("Failed to create group: No response model returned.");
            
            long groupId = groupRes.Models[0].Id;
            Debug.Log($"LoginManager: Successfully created Group with ID: {groupId}");

            // Create Fortress associated with this new group
            var newFortress = new Fortress { State = "Stable", Level = 1, GroupId = groupId };
            Debug.Log($"LoginManager: Inserting new Fortress for Group ID: {groupId}.");
            await SupabaseManager.Instance.From<Fortress>().Insert(newFortress);
            Debug.Log("LoginManager: Successfully created associated Fortress.");

            return groupId;
        }
        catch (Exception ex)
        {
            Debug.LogError($"LoginManager: Error in CreateNewGroup: {ex}");
            throw new Exception($"Failed to create group and fortress: {ex.Message}");
        }
    }

    private async Task<long> JoinSpecifiedGroup(long groupId)
    {
        Debug.Log($"LoginManager: Checking existence of Group ID: {groupId}.");
        // Check if group exists
        try
        {
            var groupRes = await SupabaseManager.Instance.From<Group>().Where(g => g.Id == groupId).Get();
            if (groupRes.Models.Count == 0) throw new Exception($"Group ID {groupId} not found in database.");
            Debug.Log($"LoginManager: Group ID {groupId} exists.");
        }
        catch (Exception ex)
        {
             Debug.LogError($"LoginManager: Group Existence Check Failed: {ex}");
             throw; // Propagate error up
        }

        // Check if group is full (max 4 users)
        Debug.Log($"LoginManager: Checking user count for Group ID: {groupId}.");
        try
        {
            var userCountRes = await SupabaseManager.Instance.From<User>().Where(u => u.GroupID == groupId).Get();
            int playerCount = userCountRes.Models.Count;
            Debug.Log($"LoginManager: Group ID {groupId} currently has {playerCount} members.");

            if (playerCount >= 4) throw new Exception("This Fortress is full (max capacity reached).");
        }
        catch (Exception ex)
        {
             Debug.LogError($"LoginManager: User Count Check Failed: {ex}");
             throw; // Propagate error up
        }

        return groupId;
    }

    private void EnterMainGame() 
    {
        Debug.Log("LoginManager: Success! Loading Main Game Scene.");
        SceneManager.LoadScene(mainGameSceneName);
    }

    private void UpdateStatus(string message, Color color)
    {
        if (statusText) { 
            statusText.text = message; 
            statusText.color = color; 
            Debug.Log($"LoginManager: Status updated: {message}");
        }
    }

    private void SetLoadingState(bool isLoading)
    {
        loginButton.interactable = !isLoading;
        registerButton.interactable = !isLoading;
        if (isLoading) Debug.Log("LoginManager: Setting loading state to TRUE.");
        else Debug.Log("LoginManager: Setting loading state to FALSE.");
    }
}
