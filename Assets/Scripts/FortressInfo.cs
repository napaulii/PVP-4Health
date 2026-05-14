using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using SupabaseModels;
using User = SupabaseModels.User; // Resolving ambiguity

public class FortressInfo : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI groupInfoText;

    private async void Start()
    {
        if (groupInfoText == null)
        {
            //Debug.LogError("Fortress Info: TextMeshPro reference is missing!");
            return;
        }

        await RefreshSquadList();
    }

    public async Task RefreshSquadList()
    {
        try
        {
            var currentUser = SupabaseManager.Instance.Auth.CurrentUser;
            if (currentUser == null) return;

            // 1. Fetch YOUR profile to get your Group ID
            var myProfileResponse = await SupabaseManager.Instance
                .From<User>()
                .Where(u => u.Id == currentUser.Id)
                .Single();

            if (myProfileResponse == null || myProfileResponse.GroupID == null)
            {
                groupInfoText.text = "No Fortress assigned.";
                return;
            }

            long myGroupId = (long)myProfileResponse.GroupID;

            // 2. Fetch EVERYONE who has the same Group ID
            var squadResponse = await SupabaseManager.Instance
                .From<User>()
                .Where(u => u.GroupID == myGroupId)
                .Get();

            List<User> squadMembers = squadResponse.Models;

            // 3. Format the display string
            string displayResult = "";
            for (int i = 0; i < squadMembers.Count; i++)
            {
                // Format: Friend1: nickname
                displayResult += $"Friend{i + 1}: {squadMembers[i].Nickname}\n";
            }

            // 4. Add the Fortress ID (which is the Group ID)
            displayResult += $"Fortress ID: {myGroupId}";

            groupInfoText.text = displayResult;
            Debug.Log($"<color=green>[Squad]</color> Successfully fetched {squadMembers.Count} squad members.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SquadDisplay] Failed to fetch squad: {ex.Message}");
            groupInfoText.text = "Error loading squad.";
        }
    }
}