using UnityEngine;
using System.Threading.Tasks;
using SupabaseModels;
using TMPro;

public class FortressUpdateScript : MonoBehaviour
{
    [Header("3D Models (Lowest Level to Highest)")]
    [Tooltip("Element 0: Tent, Element 1: Cabin, Element 2: House")]
    public GameObject[] fortresses;

    [Header("Level Requirements")]
    [Tooltip("How much total Group XP is needed to unlock each model?")]
    public int[] xpThresholds; // e.g., 0, 5000, 15000

    private UserController _userController = new UserController();

    public static event System.Action<int> OnLevelReached;
    public TextMeshProUGUI text;

    public static int CurrentLevel { get; private set; } = 0;

    private void Start()
    {
        // Fetch XP and update the model as soon as the game starts
        _ = UpdateFortressModelAsync();
    }


    void OnEnable()
    {
        FortressUpdateScript.OnLevelReached += UpdateDisplay;
        // Show current level immediately on load
        UpdateDisplay(FortressUpdateScript.CurrentLevel);
    }

    void OnDisable() => FortressUpdateScript.OnLevelReached -= UpdateDisplay;

    void UpdateDisplay(int level)
    {
        // +1 so it shows 1, 2, 3 instead of 0, 1, 2
        text.text = (level + 1).ToString();
        
    }

    /// <summary>
    /// Fetches the group's total XP from Supabase and turns on the correct 3D model.
    /// </summary>
    public async Task UpdateFortressModelAsync()
    {
        int totalGroupXp = await GetTotalGroupXpAsync();
        int targetLevel = CalculateLevel(totalGroupXp);

        if (targetLevel > CurrentLevel)
            OnLevelReached?.Invoke(targetLevel);
        CurrentLevel = targetLevel;

        Debug.Log($"[Fortress] Total Group XP: {totalGroupXp} | Unlocked Level: {targetLevel}");

        // Loop through the array and only activate the model that matches the target level
        for (int i = 0; i < fortresses.Length; i++)
        {
            if (fortresses[i] != null)
            {
                fortresses[i].SetActive(i == targetLevel);
            }
        }
    }

    /// <summary>
    /// Queries Supabase for all users in the current user's group and sums their XP.
    /// </summary>
    private async Task<int> GetTotalGroupXpAsync()
    {
        // 1. Get the current user to find out what group they are in
        var currentUser = await _userController.GetCurrentUserAsync();
        if (currentUser == null || currentUser.GroupID <= 0) return 0;

        // 2. Fetch all users that belong to that specific Group ID
        var response = await SupabaseManager.Instance.From<User>()
            .Where(x => x.GroupID == currentUser.GroupID)
            .Get();

        // 3. Sum the XP of all group members
        int sum = 0;
        foreach (var groupMember in response.Models)
        {
            sum += groupMember.Xp;
        }

        return sum;
    }

    /// <summary>
    /// Compares the total XP against your defined thresholds to find the highest unlocked level.
    /// </summary>
    private int CalculateLevel(int totalXp)
    {
        if (fortresses == null || fortresses.Length == 0)
        {
            Debug.LogError("[Fortress] Fortresses array is empty - assign in Inspector!");
            return 0;
        }

        int level = 0;
        for (int i = 0; i < xpThresholds.Length; i++)
        {
            if (totalXp >= xpThresholds[i])
                level = i;
        }

        return Mathf.Clamp(level, 0, fortresses.Length - 1);
    }
}