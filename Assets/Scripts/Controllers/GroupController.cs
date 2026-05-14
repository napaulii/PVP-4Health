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
}