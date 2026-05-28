using UnityEngine;
using System.Threading.Tasks;
using SupabaseModels;

public class FortressUpdateScript : MonoBehaviour
{
    [Header("3D Models (Lowest Level to Highest)")]
    [Tooltip("Element 0: Tent, Element 1: Cabin, Element 2: House")]
    public GameObject[] fortresses;

    [Header("Level Requirements")]
    [Tooltip("How much total Group XP is needed to unlock each model?")]
    public int[] xpThresholds; // e.g., 0, 5000, 15000

    private UserController _userController = new UserController();

    private void Start()
    {
        // Fetch XP and update the model as soon as the game starts
        _ = UpdateFortressModelAsync();
    }

    /// <summary>
    /// Fetches the group's total XP from Supabase and turns on the correct 3D model.
    /// </summary>
    public async Task UpdateFortressModelAsync()
    {
        int totalGroupXp = await GetTotalGroupXpAsync();
        int targetLevel = CalculateLevel(totalGroupXp);

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
        int level = 0;

        for (int i = 0; i < xpThresholds.Length; i++)
        {
            if (totalXp >= xpThresholds[i])
            {
                level = i;
            }
        }

        // Safety check to ensure we don't try to load an index outside our array size
        return Mathf.Clamp(level, 0, fortresses.Length - 1);
    }
}