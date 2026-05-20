using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SupabaseModels; 
using Supabase.Gotrue;

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

    [Header("Page 2: Categories")]
    [SerializeField] private Toggle category1Toggle; // E.g., category_id = 1
    [SerializeField] private Toggle category2Toggle; // E.g., category_id = 2
    [SerializeField] private Toggle category3Toggle; // E.g., category_id = 3
    private const int CATEGORY_PAGE_INDEX = 1; // Assuming Page 2 is index 1

    [Header("Page 3: Fortress (Multiplayer)")]
    [SerializeField] private Toggle joinExistingToggle; 
    [SerializeField] private TMP_InputField joinGroupIdInput; // Input for joining
    [SerializeField] private TMP_InputField newGroupNameInput; // Input for creating

    [Header("Group UI Elements")]
    [SerializeField] private GameObject groupPanel;
    [SerializeField] private TMPro.TMP_InputField joinGroupIdInput2;
    [SerializeField] private TMPro.TMP_InputField createGroupNameInput;
    [SerializeField] private UnityEngine.UI.Button groupSubmitButton;

    [Header("Google Login")]
    [SerializeField] private Button googleLoginButton;
    private string redirectUrl = "fortressgame://login-callback";

    // Temporary reference to hold cached data during UI interaction
    private User cachedCurrentUserProfile;

    private int currentSurveyPage = 0;

    private void Start()
    {
        Debug.Log("LoginManager: Starting initialization.");

        // Listen for deep links while the app is running
        Application.deepLinkActivated += OnDeepLinkActivated;

        // Check if the app was STARTED by a deep link
        if (!string.IsNullOrEmpty(Application.absoluteURL))
        {
            OnDeepLinkActivated(Application.absoluteURL);
        }

        // Add listener for Google button
        if(googleLoginButton != null)
            googleLoginButton.onClick.AddListener(OnGoogleLoginClicked);

        loginButton.onClick.AddListener(() => OnAuthClicked(false));
        registerButton.onClick.AddListener(() => OnAuthClicked(true));
        nextButton.onClick.AddListener(NextSurveyPage);
        finishSurveyButton.onClick.AddListener(OnFinishSurveyClicked);

        // Add listeners to category toggles to validate the Next button
        if (category1Toggle) category1Toggle.onValueChanged.AddListener(delegate { ValidateSurveyPage(); });
        if (category2Toggle) category2Toggle.onValueChanged.AddListener(delegate { ValidateSurveyPage(); });
        if (category3Toggle) category3Toggle.onValueChanged.AddListener(delegate { ValidateSurveyPage(); });

        loginPanel.SetActive(true);
        surveyPanel.SetActive(false);
        
        Debug.Log("LoginManager: Initializing UI state.");
        SetLoadingState(false);
    }

    private async void OnGoogleLoginClicked()
    {
        SetLoadingState(true);
        try
        {
            Debug.Log("LoginManager: Initiating Google OAuth...");

            var providerAuth = await SupabaseManager.Instance.Auth.SignIn(
                Supabase.Gotrue.Constants.Provider.Google,
                new SignInOptions { RedirectTo = redirectUrl }
            );

            if (providerAuth != null && providerAuth.Uri != null)
            {
                Debug.Log($"LoginManager: Opening Login URL: {providerAuth.Uri}");
                Application.OpenURL(providerAuth.Uri.ToString());
            }
            else
            {
                throw new Exception("Failed to generate OAuth URI.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"LoginManager: Google Login Failed: {ex.Message}");
            UpdateStatus("Google Login Failed", Color.red);
            SetLoadingState(false);
        }
    }

    private async void OnDeepLinkActivated(string url)
    {
        Debug.Log($"LoginManager: Deep Link received: {url}");
    
        try
        {
            await SupabaseManager.Instance.Auth.GetSessionFromUrl(new Uri(url));
            
            if (SupabaseManager.Instance.Auth.CurrentUser != null)
            {
                Debug.Log("LoginManager: Mobile session established successfully.");
                await DirectUserFlow();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"LoginManager: Failed to process deep link session: {ex.Message}");
            SetLoadingState(false);
        }
    }

    private async void CheckSessionAfterLogin()
    {
        if (SupabaseManager.Instance.Auth.CurrentUser != null)
        {
            await DirectUserFlow();
        }
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
                cachedCurrentUserProfile = userResponse.Models[0];
                Debug.Log("LoginManager: User profile found. Checking Group alignment...");

                if (cachedCurrentUserProfile.GroupID == null || cachedCurrentUserProfile.GroupID == 0)
                {
                    Debug.Log("LoginManager: User does not belong to a group. Opening Group Panel.");
                    
                    groupSubmitButton.onClick.RemoveAllListeners();
                    groupSubmitButton.onClick.AddListener(() => _ = OnGroupSubmit());
                    
                    groupPanel.SetActive(true);
                }
                else
                {
                    Debug.Log("LoginManager: User profile found with group. Proceeding to Main Game.");
                    EnterMainGame();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"LoginManager: Error during user flow check: {ex}");
            UpdateStatus("Failed to retrieve user data after login.", Color.red);
            SetLoadingState(false);
        }
    }

    private async Task OnGroupSubmit()
    {
        if (cachedCurrentUserProfile == null)
        {
            Debug.LogError("LoginManager: Cached user profile is missing during submission.");
            return;
        }

        string joinIdText = joinGroupIdInput2.text.Trim();
        string createNameText = createGroupNameInput.text.Trim();
        long targetGroupId = 0;

        SetLoadingState(true);
        UpdateStatus("Processing group alignment...", Color.white);

        try
        {
            if (!string.IsNullOrEmpty(joinIdText))
            {
                if (long.TryParse(joinIdText, out long parsedGroupId))
                {
                    UpdateStatus("Checking existing group...", Color.white);
                    targetGroupId = await JoinSpecifiedGroup(parsedGroupId);
                }
                else
                {
                    throw new Exception("Group ID must be a valid number.");
                }
            }
            else if (!string.IsNullOrEmpty(createNameText))
            {
                UpdateStatus("Creating new group...", Color.white);
                targetGroupId = await CreateNewGroup(cachedCurrentUserProfile.Id, createNameText);
            }
            else
            {
                throw new Exception("Please enter either a Group ID to join or a Name to create one.");
            }

            UpdateStatus("Updating user profile...", Color.white);
            cachedCurrentUserProfile.GroupID = targetGroupId;

            await SupabaseManager.Instance
                .From<User>()
                .Where(u => u.Id == cachedCurrentUserProfile.Id)
                .Update(cachedCurrentUserProfile);

            Debug.Log($"LoginManager: User successfully updated with Group ID: {targetGroupId}");
            
            groupPanel.SetActive(false);
            SetLoadingState(false);
            EnterMainGame();
        }
        catch (Exception ex)
        {
            Debug.LogError($"LoginManager: Error during group assignment process: {ex.Message}");
            UpdateStatus(ex.Message, Color.red);
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

        ValidateSurveyPage();
    }

    // Controls the interactability of the 'Next' button depending on the current page's requirements
    private void ValidateSurveyPage()
    {
        if (currentSurveyPage == CATEGORY_PAGE_INDEX)
        {
            // Requires at least one toggle to be ON
            bool hasSelectedCategory = category1Toggle.isOn || category2Toggle.isOn || category3Toggle.isOn;
            nextButton.interactable = hasSelectedCategory;
        }
        else
        {
            // Default rule for other pages
            nextButton.interactable = true;
        }
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
            try
            {
                 await SupabaseManager.Instance.From<User>().Insert(newProfile);
                 Debug.Log($"LoginManager: Successfully inserted new User record for {userId}.");
            }
            catch (Exception e)
            {
                Debug.LogError($"LoginManager: FAILED TO INSERT USER PROFILE: {e.Message}");
                throw; 
            }

            // 2. Assign selected categories to the user_category joining table
            List<UserCategory> selectedCategories = new List<UserCategory>();
            
            // Note: Map the CategoryId integers to match the actual IDs in your `public.category` table.
            if (category1Toggle.isOn) selectedCategories.Add(new UserCategory { UserId = userId, CategoryId = 1 });
            if (category2Toggle.isOn) selectedCategories.Add(new UserCategory { UserId = userId, CategoryId = 2 });
            if (category3Toggle.isOn) selectedCategories.Add(new UserCategory { UserId = userId, CategoryId = 3 });

            if (selectedCategories.Count > 0)
            {
                try
                {
                    await SupabaseManager.Instance.From<UserCategory>().Insert(selectedCategories);
                    Debug.Log("LoginManager: Successfully assigned categories to user.");
                }
                catch (Exception e)
                {
                    Debug.LogError($"LoginManager: FAILED TO INSERT USER CATEGORIES: {e.Message}");
                    throw;
                }
            }

            // 3. Now handle the Group creation/joining
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

            // 4. Update the User with the Group ID
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
                throw; 
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

        var newGroup = new Group { Title = title, UserId = userId };
        Debug.Log($"LoginManager: Inserting new Group '{title}' for user {userId}.");
        try
        {
            var groupRes = await SupabaseManager.Instance
                .From<Group>()
                .Insert(newGroup, new Postgrest.QueryOptions { Returning = Postgrest.QueryOptions.ReturnType.Representation });
            
            if (groupRes.Models.Count == 0) throw new Exception("Failed to create group: No response model returned.");
            
            long groupId = groupRes.Models[0].Id;
            Debug.Log($"LoginManager: Successfully created Group with ID: {groupId}");

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
        try
        {
            var groupRes = await SupabaseManager.Instance.From<Group>().Where(g => g.Id == groupId).Get();
            if (groupRes.Models.Count == 0) throw new Exception($"Group ID {groupId} not found in database.");
            Debug.Log($"LoginManager: Group ID {groupId} exists.");
        }
        catch (Exception ex)
        {
             Debug.LogError($"LoginManager: Group Existence Check Failed: {ex}");
             throw;
        }

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
             throw; 
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