using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject homePanel;
    public GameObject shopPanel;
    public GameObject habitsPanel;
    public GameObject achievementsPanel;
    public GameObject challengePanel;
    public GameObject settingsPanel;

    public void OpenShop()
    {
        homePanel.SetActive(false);
        shopPanel.SetActive(true);
    }

    public void OpenHome()
    {
        shopPanel.SetActive(false);
        habitsPanel.SetActive(false);
        achievementsPanel.SetActive(false);
        challengePanel.SetActive(false);
        settingsPanel.SetActive(false);
        homePanel.SetActive(true);
    }

    public void OpenHabits()
    {
        homePanel.SetActive(false);
        habitsPanel.SetActive(true);
    }

    public void OpenAchievements()
    {
        homePanel.SetActive(false);
        achievementsPanel.SetActive(true);
    }

    public void OpenChallenges()
    {
        homePanel.SetActive(false);
        challengePanel.SetActive(true);
    }

    public void OpenSettings()
    {
        homePanel.SetActive(false);
        settingsPanel.SetActive(true);
    }
}
