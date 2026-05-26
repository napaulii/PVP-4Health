using UnityEngine;

public class TabManager : MonoBehaviour
{
    public GameObject personalPanel;
    public GameObject groupPanel;

    public void ShowPersonalChallenges()
    {
        personalPanel.SetActive(true);
        groupPanel.SetActive(false);
    }

    public void ShowGroupChallenges()
    {
        personalPanel.SetActive(false);
        groupPanel.SetActive(true);
    }
}