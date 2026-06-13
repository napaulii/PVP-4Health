using UnityEngine;
using UnityEngine.SceneManagement;

public class BottomNavManager : MonoBehaviour
{
    public static string targetPanel = "Home";

    public void OpenHome()
    {
        targetPanel = "Home";
        if (SceneManager.GetActiveScene().name != "Home")
            SceneManager.LoadScene("Home");
        else
            FindFirstObjectByType<UIManager>()?.OpenHome();
    }

    public void OpenShop()
    {
        targetPanel = "Shop";
        if (SceneManager.GetActiveScene().name != "Home")
            SceneManager.LoadScene("Home");
        else
            FindFirstObjectByType<UIManager>()?.OpenShop();
    }

    public void OpenAchievements()
    {
        targetPanel = "Achievements";
        if (SceneManager.GetActiveScene().name != "Home")
            SceneManager.LoadScene("Home");
        else
            FindFirstObjectByType<UIManager>()?.OpenAchievements();
    }

    public void OpenSettings()
    {
        targetPanel = "Settings";
        if (SceneManager.GetActiveScene().name != "Home")
            SceneManager.LoadScene("Home");
        else
            FindFirstObjectByType<UIManager>()?.OpenSettings();
    }

    public void OpenHabit()
    {
        SceneManager.LoadScene("Habit");
    }
}