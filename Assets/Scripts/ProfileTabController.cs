using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProfileTabController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject profileTile;
    public GameObject goalTile;
    public GameObject historyTile;

    [Header("Profile tab buttons (one per tile)")]
    public Button profileBtn_inProfile;  
    public Button profileBtn_inGoal;     
    public Button profileBtn_inHistory;  

    [Header("Goals tab buttons")]
    public Button goalBtn_inProfile;     
    public Button goalBtn_inGoal;       
    public Button goalBtn_inHistory;    

    [Header("History tab buttons")]
    public Button historyBtn_inProfile; 
    public Button historyBtn_inGoal;    
    public Button historyBtn_inHistory; 

    [Header("Tab Styling")]
    public Color activeColor = new Color(0.75f, 0.22f, 0.65f, 1f);
    public Color inactiveColor = Color.white;

   
    private Button[] profileBtns, goalBtns, historyBtns;

    void Start()
    {
        profileBtns = new[] { profileBtn_inProfile, profileBtn_inGoal, profileBtn_inHistory };
        goalBtns = new[] { goalBtn_inProfile, goalBtn_inGoal, goalBtn_inHistory };
        historyBtns = new[] { historyBtn_inProfile, historyBtn_inGoal, historyBtn_inHistory };

        
        foreach (var btn in profileBtns) if (btn) btn.onClick.AddListener(() => ShowTab(0));
        foreach (var btn in goalBtns) if (btn) btn.onClick.AddListener(() => ShowTab(1));
        foreach (var btn in historyBtns) if (btn) btn.onClick.AddListener(() => ShowTab(2));

        ShowTab(0);
    }

    public void ShowTab(int index)
    {
        profileTile.SetActive(index == 0);
        goalTile.SetActive(index == 1);
        historyTile.SetActive(index == 2);

       
        HighlightGroup(profileBtns, index == 0);
        HighlightGroup(goalBtns, index == 1);
        HighlightGroup(historyBtns, index == 2);
    }

    void HighlightGroup(Button[] btns, bool on)
    {
        foreach (var btn in btns)
        {
            if (!btn) continue;
            var img = btn.GetComponent<Image>();
            if (img) img.color = on ? activeColor : inactiveColor;

            var lbl = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl) lbl.fontStyle = on ? FontStyles.Bold : FontStyles.Normal;
        }
    }
}