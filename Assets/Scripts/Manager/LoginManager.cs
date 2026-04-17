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
        UpdateStatus("Finalizing Fortress setup...", Color.cyan);

        try
        {
            string userId = SupabaseManager.Instance.Auth.CurrentUser.Id;
            string email = SupabaseManager.Instance.Auth.CurrentUser.Email;

            // 1. Handle Group Assignment
            long assignedGroupId;

            if (joinExistingToggle.isOn)
            {
                if (!long.TryParse(joinGroupIdInput.text, out long targetId))
                {
                    UpdateStatus("Invalid Group ID format.", Color.red);
                    SetLoadingState(false);
                    return;
                }
                assignedGroupId = await JoinSpecifiedGroup(targetId);
            }
            else
            {
                assignedGroupId = await CreateNewGroup(userId, newGroupNameInput.text);
            }

            // 2. Create User Profile
            var newProfile = new User {
                Id = userId, Email = email,
                FirstName = firstNameInput.text, LastName = lastNameInput.text,
                Nickname = nicknameInput.text, GroupID = assignedGroupId
            };
            await SupabaseManager.Instance.From<User>().Insert(newProfile);

            // 3. Create First Challenge
            await CreateInitialChallenge(userId);
            
            EnterMainGame();
        }
        catch (Exception ex)
        {
            UpdateStatus(ex.Message, Color.red);
            SetLoadingState(false);
        }
    }

    private async Task<long> CreateNewGroup(string userId, string title)
    {
        if (string.IsNullOrEmpty(title)) title = "The New Bastion";

        var newGroup = new Group { Title = title, UserId = userId };
        var groupRes = await SupabaseManager.Instance.From<Group>().Insert(newGroup);
        
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