using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject homePanel;
    public GameObject shopPanel;

    public void OpenShop()
    {
        homePanel.SetActive(false);
        shopPanel.SetActive(true);
    }

    public void OpenHome()
    {
        shopPanel.SetActive(false);
        homePanel.SetActive(true);
    }
}
