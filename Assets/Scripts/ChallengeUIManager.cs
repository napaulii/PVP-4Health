using SupabaseModels;
using System.Collections.Generic;
using UnityEngine;

public class ChallengeUIManager : MonoBehaviour
{
    [Header("Personal Rows (Daily)")]
    public ChallengeRowUI[] personalRows;

    [Header("Group Rows (Weekly)")]
    public GroupChallengeRowUI[] groupRows;

    [Header("References")]
    public ChallengeActions actionManager;

    private UserChallengeController _personalCtrl = new UserChallengeController();
    private GroupChallengeController _groupCtrl = new GroupChallengeController();
    private UserController _userCtrl = new UserController();

    async void OnEnable()
    {
        List<UserChallenge> personal = await _personalCtrl.GetAllUserChallengesAsync();

        for (int i = 0; i < personalRows.Length; i++)
        {
            if (personalRows[i] == null) continue;
            personalRows[i].gameObject.SetActive(true);

            if (i < personal.Count)
                personalRows[i].Setup(personal[i], actionManager, this);
            else
                personalRows[i].SetEmpty();
        }

        if (groupRows == null || groupRows.Length == 0) return;

        var currentUser = await _userCtrl.GetCurrentUserAsync();
        if (currentUser == null || currentUser.GroupID <= 0)
        {
            foreach (var row in groupRows)
                if (row != null) { row.gameObject.SetActive(true); row.SetEmpty(); }
            return;
        }

        List<GroupChallenge> group = await _groupCtrl.GetGroupChallengesAsync();

        for (int i = 0; i < groupRows.Length; i++)
        {
            if (groupRows[i] == null) continue;
            groupRows[i].gameObject.SetActive(true);

            if (i < group.Count)
                groupRows[i].Setup(group[i], actionManager, this);
            else
                groupRows[i].SetEmpty();
        }
    }

    public void CollapseAllOtherRows(ChallengeRowUI current)
    {
        foreach (var row in personalRows)
            if (row != current && row != null) row.CloseDetails();
    }

    public void CollapseAllRows()
    {
        foreach (var row in personalRows)
            if (row != null) row.CloseDetails();
    }

    public void RefreshUI() => OnEnable();
}