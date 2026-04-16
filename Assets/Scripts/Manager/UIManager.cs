using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject homePanel;
    public GameObject shopPanel;
    public GameObject habitsPanel;
    public GameObject achievementsPanel;
    public GameObject addChallengesPanel;
    public GameObject profilePanel;

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
        addChallengesPanel.SetActive(false);
        profilePanel.SetActive(false);
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

    public void OpenAddChallenges()
    {
        homePanel.SetActive(false);
        addChallengesPanel.SetActive(true);
    }

    public void OpenProfile()
    {
        homePanel.SetActive(false);
        profilePanel.SetActive(true);
    }
}
