using UnityEngine;

public class BuildCategories : MonoBehaviour
{
    public GameObject decors;
    public GameObject fortresses;

    private FortressItemsDisplay _fortressDisplay;

    private void Start()
    {
        _fortressDisplay = fortresses.GetComponentInChildren<FortressItemsDisplay>();
        decors.SetActive(true);
        fortresses.SetActive(false);
    }

    public void OpenDecorations()
    {
        decors.SetActive(true);
        fortresses.SetActive(false);
    }

    public void OpenFortresses()
    {
        decors.SetActive(false);
        fortresses.SetActive(true);

        // Force refresh every time the tab opens
        if (_fortressDisplay != null)
            _fortressDisplay.Refresh();
    }
}