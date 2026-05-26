using UnityEngine;
using SupabaseModels;

public class GroupChallengeUIManager : MonoBehaviour
{
    public GroupChallengeRowUI[] rows; // This should be an array (plural)
    public ChallengeActions actionManager; // This is the missing field

    private GroupChallengeController _groupCtrl = new GroupChallengeController();
    private UserController _userCtrl = new UserController();

    async void OnEnable()
    {
        var currentUser = await _userCtrl.GetCurrentUserAsync();

        if (currentUser == null || currentUser.GroupID <= 0)
        {
            foreach (var row in rows) row.gameObject.SetActive(false);
            return;
        }

        GroupChallenge activeChallenge = await _groupCtrl.GetOrCreateWeeklyGroupChallengeAsync((long)currentUser.GroupID);

        if (activeChallenge != null && rows.Length > 0)
        {
            rows[0].gameObject.SetActive(true);
            rows[0].Setup(activeChallenge, actionManager, this);
        }
    }

    public void CollapseAllOtherRows(GroupChallengeRowUI currentActiveRow)
    {
        foreach (var row in rows)
        {
            if (row != currentActiveRow && row.gameObject.activeSelf)
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