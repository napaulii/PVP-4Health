using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;

public class GroupController
{
    // CREATE
    public async Task<Group> CreateGroupAsync(string title)
    {
        try
        {
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;

            var newGroup = new Group
            {
                Title = title,
                UserId = currentUserId // The creator of the group
            };

            var response = await SupabaseManager.Instance.From<Group>().Insert(newGroup);
            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating group: {e.Message}");
            return null;
        }


    }

    // GET all users in the same group as the current user
    public async Task<List<User>> GetGroupMembersAsync()
    {
        try
        {
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;

            // First get current user to find their group
            var currentUserResponse = await SupabaseManager.Instance.From<User>()
                .Where(x => x.Id == currentUserId)
                .Get();

            if (currentUserResponse.Models.Count == 0)
            {
                Debug.LogWarning("Current user not found.");
                return new List<User>();
            }

            long? groupId = currentUserResponse.Models[0].GroupID;

            if (groupId == null)
            {
                Debug.LogWarning("Current user is not in a group.");
                return new List<User>();
            }

            // Get all users in the same group
            var membersResponse = await SupabaseManager.Instance.From<User>()
                .Where(x => x.GroupID == groupId)
                .Get();

            return membersResponse.Models;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching group members: {e.Message}");
            return new List<User>();
        }
    }

    // GET all PersonalItems purchased by anyone in the current user's group
    public async Task<List<PersonalItem>> GetGroupPurchasedItemsAsync()
    {
        try
        {
            List<User> members = await GetGroupMembersAsync();

            if (members.Count == 0)
                return new List<PersonalItem>();

            var allItems = new List<PersonalItem>();

            foreach (User member in members)
            {
                var itemsResponse = await SupabaseManager.Instance.From<PersonalItem>()
                    .Where(x => x.UserId == member.Id)
                    .Get();

                if (itemsResponse.Models != null)
                    allItems.AddRange(itemsResponse.Models);
            }

            return allItems;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching group purchased items: {e.Message}");
            return new List<PersonalItem>();
        }
    }
}