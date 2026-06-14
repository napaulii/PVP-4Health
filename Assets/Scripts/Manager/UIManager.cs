using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject homePanel;
    public GameObject homeBottomPanel;
    
    public GameObject shopPanel;
    public GameObject achievementsPanel;
    public GameObject profilePanel;
    public GameObject settingsPanel;
    public GameObject BuildModePanel;
    public GameObject BottomNavBar;

    [SerializeField] private PlacementSystem stp;
    

    void Start()
    {
        // React to what BottomNavManager requested
        switch (BottomNavManager.targetPanel)
        {
            case "Shop": OpenShop(); break;
            case "Achievements": OpenAchievements(); break;
            case "Settings": OpenSettings(); break;
            case "Profile": OpenProfile(); break;
            default: OpenHome(); break;
        }
    }

    public void OpenShop()
    {
        shopPanel.SetActive(true);
        achievementsPanel.SetActive(false);
        profilePanel.SetActive(false);
        settingsPanel.SetActive(false);
        
        homePanel.SetActive(false);
        BuildModePanel.SetActive(false);
        BottomNavBar.SetActive(true);
    }

    public void OpenHome()
    {
       
        shopPanel.SetActive(false);
        achievementsPanel.SetActive(false);
        profilePanel.SetActive(false);
        settingsPanel.SetActive(false);
        
        homePanel.SetActive(true);
        BuildModePanel.SetActive(false);
        BottomNavBar.SetActive(true);
        stp.StopPlacement();
    }

    public void OpenHabit()
    {
        SceneManager.LoadScene("Habit");
    }

    public void OpenAchievements()
    {
        shopPanel.SetActive(false);
        achievementsPanel.SetActive(true);
        profilePanel.SetActive(false);
        settingsPanel.SetActive(false);
        
        homePanel.SetActive(false);
        BuildModePanel.SetActive(false);
        BottomNavBar.SetActive(true);
        
    }

    public void OpenProfile()
    {
        homePanel.SetActive(false);
        
        profilePanel.SetActive(true);
        BuildModePanel.SetActive(false);
        BottomNavBar.SetActive(false);
        
    }

    public void OpenSettings()
    {
        shopPanel.SetActive(false);
        achievementsPanel.SetActive(false);
        profilePanel.SetActive(false);
        settingsPanel.SetActive(true);
        
        homePanel.SetActive(false);
        BuildModePanel.SetActive(false);
        BottomNavBar.SetActive(true);
       
    }

    public void OpenBuildMode()
    {
        
        shopPanel.SetActive(false);
        achievementsPanel.SetActive(false);
        profilePanel.SetActive(false);
        settingsPanel.SetActive(false);
        
        homePanel.SetActive(false);
        BuildModePanel.SetActive(true);
        BottomNavBar.SetActive(false);
    }
}