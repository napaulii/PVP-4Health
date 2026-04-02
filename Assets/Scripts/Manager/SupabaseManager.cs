using UnityEngine;
using Supabase;
using System.Threading.Tasks;

public class SupabaseManager : MonoBehaviour
{
    public static Client Instance; // Singleton so you can access it anywhere

    private readonly string url = "https://tqrbelbyudxdoovbybar.supabase.co";
    private readonly string anonKey = "sb_publishable_jQBPOXy5EnBORYkKLEhcEw_bJN9yUgY";

    async void Awake()
    {
        if (Instance == null)
        {
            Instance = new Client(url, anonKey);
            await Instance.InitializeAsync();
            Debug.Log("Supabase Initialized!");
            DontDestroyOnLoad(gameObject); // Keeps the connection alive between scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }
}