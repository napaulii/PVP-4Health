using System;
using System.Threading.Tasks;
using UnityEngine;

public class Autologin : MonoBehaviour
{
    [SerializeField] private string testEmail = "testuser@gmail.com";
    [SerializeField] private string testPassword = "password123";

    private async Task TestLogin()
    {
        try
        {
            await SupabaseManager.Instance.Auth.SignIn(testEmail, testPassword);
            Debug.Log($"User logged in successfully! ID: {SupabaseManager.Instance.Auth.CurrentUser.Id}");
        }
        
        catch (Exception loginErr)
        {
            Debug.LogError($"Login failed: {loginErr.Message}");
        }
    }
    
}
