using SupabaseModels;
using System.Collections.Generic;
using UnityEngine;

public class ChallengeUIManager : MonoBehaviour
{
    public ChallengeRowUI[] rows;
    public ChallengeActions actionManager;

    // --- ADD THIS: Reference to the Group Manager ---
    public GroupChallengeUIManager groupUIManager;

    private UserChallengeController _userChallengeController = new UserChallengeController();

    async void OnEnable()
    {
        string userId = SupabaseManager.Instance.Auth.CurrentUser.Id;
        List<UserChallenge> userChallenges = await _userChallengeController.GetAllUserChallengesAsync();

        for (int i = 0; i < rows.Length; i++)
        {
            if (i < userChallenges.Count)
            {
                rows[i].gameObject.SetActive(true);
                rows[i].Setup(userChallenges[i], actionManager, this);
            }
            else
            {
                rows[i].gameObject.SetActive(false);
            }
        }
    }

    public void CollapseAllOtherRows(ChallengeRowUI currentActiveRow)
    {
        // 1. Close all other PERSONAL rows
        foreach (var row in rows)
        {
            if (row != currentActiveRow && row.gameObject.activeSelf && row.detailsArea != null)
            {
                row.detailsArea.SetActive(false);
            }
        }

        // 2. Tell the GROUP manager to close all its rows too
        if (groupUIManager != null)
        {
            groupUIManager.CollapseAllRows();
        }
    }

    // --- ADD THIS: A method for the Group manager to call ---
    public void CollapseAllRows()
    {
        foreach (var row in rows)
        {
            if (row.gameObject.activeSelf && row.detailsArea != null)
            {
                row.detailsArea.SetActive(false);
            }
        }
    }

    public void RefreshUI()
    {
        OnEnable();
    }
}