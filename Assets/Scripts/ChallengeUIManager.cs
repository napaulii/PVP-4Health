using SupabaseModels;
using System.Collections.Generic;
using UnityEngine;

public class ChallengeUIManager : MonoBehaviour
{
    public ChallengeRowUI[] rows; // Drag your 3 static PChallengeRows here
    public ChallengeActions actionManager;

    private UserChallengeController _userChallengeController = new UserChallengeController();

    async void OnEnable() // Refresh when the window opens
    {
        string userId = SupabaseManager.Instance.Auth.CurrentUser.Id;

        // Use your existing ChallengeController method that has the .Select("*, Challenge(*)")
        List<UserChallenge> userChallenges = await _userChallengeController.GetAllUserChallengesAsync();

        // Map the database rows to your 3 UI rows
        for (int i = 0; i < rows.Length; i++)
        {
            if (i < userChallenges.Count)
            {
                rows[i].gameObject.SetActive(true);
                rows[i].Setup(userChallenges[i], actionManager);
            }
            else
            {
                rows[i].gameObject.SetActive(false);
            }
        }
    }
    public void RefreshUI()
    {
        // This just triggers the OnEnable logic again to fetch fresh data from DB
        OnEnable();
    }
}