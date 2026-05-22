using UnityEngine;
using UnityEngine.UI;

public class ProfileManager : MonoBehaviour
{
    public GameObject profilePanel;
    public GameObject goalsPanel;
    public GameObject historyPanel;

    public void OpenProfile()
    {
        goalsPanel.SetActive(false);
        historyPanel.SetActive(false);
        profilePanel.SetActive(true);
    }
}

