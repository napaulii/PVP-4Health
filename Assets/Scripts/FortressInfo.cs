using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using SupabaseModels;
using User = SupabaseModels.User;
using Group = SupabaseModels.Group; // Reference the new model

public class FortressInfo : MonoBehaviour
{
    [Header("Own Profile UI")]
    [SerializeField] private TextMeshProUGUI ownNicknameText;
    [SerializeField] private TextMeshProUGUI ownEmailText;

    [Header("Squad UI References")]
    [Tooltip("Assign the 4 friend GameObjects here in order.")]
    [SerializeField] private GameObject[] squadSlots; 
    [SerializeField] private TextMeshProUGUI fortressIdText;

    // Keep track of the current list locally so we know who to kick by index
    private List<User> currentSquadMembers = new List<User>();

    private async void Start()
    {
        if (squadSlots == null || squadSlots.Length == 0)
        {
            Debug.LogError("Fortress Info: Squad Slots are not assigned!");
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

            var myProfileResponse = await SupabaseManager.Instance
                .From<User>()
                .Where(u => u.Id == currentUser.Id)
                .Single();

            if (myProfileResponse == null) return;

            if (ownNicknameText != null) ownNicknameText.text = "Username: " + myProfileResponse.Nickname;
            if (ownEmailText != null) ownEmailText.text = "Email: " + myProfileResponse.Email;

            if (myProfileResponse.GroupID == null)
            {
                DisableAllSlots();
                if (fortressIdText != null) fortressIdText.text = "No Fortress assigned.";
                return;
            }

            long myGroupId = (long)myProfileResponse.GroupID;
            if (fortressIdText != null) fortressIdText.text = $"Fortress ID: {myGroupId}";

            // --- NEW: Admin Check ---
            var groupData = await SupabaseManager.Instance
                .From<Group>()
                .Where(g => g.Id == myGroupId)
                .Single();
            
            bool isAdmin = groupData != null && groupData.AdminId == currentUser.Id;
            // ------------------------

            var squadResponse = await SupabaseManager.Instance
                .From<User>()
                .Where(u => u.GroupID == myGroupId)
                .Get();

            currentSquadMembers = squadResponse.Models
                .Where(u => u.Id != currentUser.Id) 
                .ToList();

            for (int i = 0; i < squadSlots.Length; i++)
            {
                if (i < currentSquadMembers.Count)
                {
                    squadSlots[i].SetActive(true);
                    UpdateSlotUI(squadSlots[i], currentSquadMembers[i].Nickname, isAdmin);
                }
                else
                {
                    squadSlots[i].SetActive(false);
                }
            }

            Debug.Log($"<color=green>[Squad]</color> Updated. Admin Status: {isAdmin}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SquadDisplay] Failed: {ex.Message}");
            DisableAllSlots();
        }
    }

    private void UpdateSlotUI(GameObject slot, string nickname, bool showKickButton)
    {
        // Set Nickname
        Transform textTransform = slot.transform.Find("Text (TMP)");
        if (textTransform != null)
        {
            var tmp = textTransform.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = nickname;
        }

        // Set Kick Button Visibility
        Transform kickBtnTransform = slot.transform.Find("KickButton");
        if (kickBtnTransform != null)
        {
            kickBtnTransform.gameObject.SetActive(showKickButton);
        }
    }

    // This is the method you call from your UI Button
    // Pass 0 for the first slot, 1 for second, etc.
    public async void KickPlayer(int index)
    {
        if (index >= currentSquadMembers.Count) return;

        User targetUser = currentSquadMembers[index];
        
        try 
        {
            // Nullify the GroupID for the kicked user
            targetUser.GroupID = null;

            await SupabaseManager.Instance
                .From<User>()
                .Update(targetUser);

            Debug.Log($"Kicked {targetUser.Nickname} successfully.");
            
            // Refresh UI
            await RefreshSquadList();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Kick failed: {ex.Message}");
        }
    }

    private void DisableAllSlots()
    {
        foreach (var slot in squadSlots)
        {
            if (slot != null) slot.SetActive(false);
        }
    }
}