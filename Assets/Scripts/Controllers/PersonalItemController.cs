using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using SupabaseModels;

public class PersonalItemController
{
    // 1. CREATE
    public async Task<PersonalItem> CreatePersonalItemAsync(string title, string description, int price)
    {
        try
        {
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;

            var newItem = new PersonalItem
            {
                Title = title, // Maps to "tittle" in DB
                Description = description,
                Price = price,
                UserId = currentUserId // Maps to "fk_userid" in DB
            };

            var response = await SupabaseManager.Instance.From<PersonalItem>().Insert(newItem);
            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating personal item: {e.Message}");
            return null;
        }
    }

    // 2. READ ALL (For Current User)
    public async Task<List<PersonalItem>> GetAllPersonalItemsAsync()
    {
        try
        {
            string currentUserId = SupabaseManager.Instance.Auth.CurrentUser.Id;

            var response = await SupabaseManager.Instance.From<PersonalItem>()
                .Where(x => x.UserId == currentUserId)
                .Get();

            return response.Models;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching personal items: {e.Message}");
            return new List<PersonalItem>();
        }
    }

    // 3. READ ONE
    public async Task<PersonalItem> GetPersonalItemByIdAsync(long itemId)
    {
        try
        {
            var response = await SupabaseManager.Instance.From<PersonalItem>()
                .Where(x => x.Id == itemId)
                .Get();

            if (response.Models.Count > 0)
            {
                return response.Models[0];
            }
            return null;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error fetching personal item by ID: {e.Message}");
            return null;
        }
    }

    // 4. UPDATE
    public async Task<PersonalItem> UpdatePersonalItemAsync(long itemId, string newTitle, string newDescription, int newPrice)
    {
        try
        {
            var itemToUpdate = await GetPersonalItemByIdAsync(itemId);

            if (itemToUpdate == null)
            {
                Debug.LogWarning("Cannot update: Personal item not found.");
                return null;
            }

            itemToUpdate.Title = newTitle;
            itemToUpdate.Description = newDescription;
            itemToUpdate.Price = newPrice;

            var response = await SupabaseManager.Instance.From<PersonalItem>().Update(itemToUpdate);
            return response.Models[0];
        }
        catch (Exception e)
        {
            Debug.LogError($"Error updating personal item: {e.Message}");
            return null;
        }
    }

    // 5. DELETE
    public async Task<bool> DeletePersonalItemAsync(long itemId)
    {
        try
        {
            await SupabaseManager.Instance.From<PersonalItem>()
                .Where(x => x.Id == itemId)
                .Delete();

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error deleting personal item: {e.Message}");
            return false;
        }
    }
}