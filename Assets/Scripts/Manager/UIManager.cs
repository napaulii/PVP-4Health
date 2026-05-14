using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject homePanel;
    public GameObject homeBottomPanel;
    public GameObject shopPanel;
    public GameObject achievementsPanel;
    public GameObject addChallengesPanel;
    public GameObject profilePanel;
    public GameObject settingsPanel;
        
    public void OpenShop()
    {
        homePanel.SetActive(false);
        homeBottomPanel.SetActive(false);
        shopPanel.SetActive(true);
    }

    public void OpenHome()
    {
        shopPanel.SetActive(false);
        achievementsPanel.SetActive(false);
        addChallengesPanel.SetActive(true);
        profilePanel.SetActive(false);
        settingsPanel.SetActive(false);
        homeBottomPanel.SetActive(true);
        homePanel.SetActive(true);
    }

    public void OpenHabit()
    {
        SceneManager.LoadScene("Habit");
    }


    public void OpenAchievements()
    {
        homePanel.SetActive(false);
        homeBottomPanel.SetActive(false);
        achievementsPanel.SetActive(true);
    }

    public void OpenAddChallenges()
    {
        homePanel.SetActive(false);
        homeBottomPanel.SetActive(false);
        addChallengesPanel.SetActive(true);
    }

    public void OpenProfile()
    {
        homePanel.SetActive(false);
        homeBottomPanel.SetActive(false);
        profilePanel.SetActive(true);
    }

    public void OpenSettings()
    {
        homePanel.SetActive(false);
        homeBottomPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }
}
