using UnityEngine;
using SupabaseModels;

public class GroupChallengeUIManager : MonoBehaviour
{
    public GroupChallengeRowUI[] rows;
    public ChallengeActions actionManager;

    // --- ADD THIS: Reference to the Personal Manager ---
    public ChallengeUIManager personalUIManager;

    private GroupChallengeController _groupCtrl = new GroupChallengeController();
    private UserController _userCtrl = new UserController();

    async void OnEnable()
    {
        var currentUser = await _userCtrl.GetCurrentUserAsync();

        if (currentUser == null || currentUser.GroupID <= 0)
        {
            foreach (var row in rows)
            {
                if (row != null) row.gameObject.SetActive(false);
            }
            return;
        }

        if (rows == null || rows.Length == 0 || rows[0] == null) return;

        GroupChallenge activeChallenge = await _groupCtrl.GetOrCreateWeeklyGroupChallengeAsync((long)currentUser.GroupID);

        if (activeChallenge != null)
        {
            rows[0].gameObject.SetActive(true);
            rows[0].Setup(activeChallenge, actionManager, this);
        }
    }

    public void CollapseAllOtherRows(GroupChallengeRowUI currentActiveRow)
    {
        // 1. Close all other GROUP rows
        foreach (var row in rows)
        {
            if (row != currentActiveRow && row.gameObject.activeSelf && row.detailsArea != null)
            {
                row.detailsArea.SetActive(false);
            }
        }

        // 2. Tell the PERSONAL manager to close all its rows too
        if (personalUIManager != null)
        {
            personalUIManager.CollapseAllRows();
        }
    }

    // --- ADD THIS: A method for the Personal manager to call ---
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