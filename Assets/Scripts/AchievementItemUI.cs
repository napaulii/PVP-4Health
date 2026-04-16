using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SupabaseModels;

public class AchievementItemUI : MonoBehaviour
{
    public TextMeshProUGUI text;
    public Button claimButton;
    public GameObject checkmark;

    public void Setup(Achievement a)
    {
        text.text = a.Title;

        claimButton.gameObject.SetActive(a.Unlocked && !a.Claimed);
        checkmark.SetActive(a.Claimed);

        claimButton.onClick.RemoveAllListeners();
        claimButton.onClick.AddListener(() =>
        {
            a.Claimed = true;

            claimButton.gameObject.SetActive(false);
            checkmark.SetActive(true);

            Debug.Log("Claimed: " + a.Title);
        });
    }
}