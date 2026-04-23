using UnityEngine;
using TMPro;
using UnityEngine.UI;
using SupabaseModels;

public class HabitListItemUI : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public Button itemButton;

    private long _myHabitId;
    private HabitManager _manager;

    public Button completeButton;
    public Image completeButtonIcon; // To change color/icon when clicked

    public void Setup(Habit habitData, HabitManager manager)
    {
        Image myImage = GetComponent<Image>();
        if (myImage != null)
        {
            myImage.enabled = true;
        }

        if (itemButton != null)
        {
            itemButton.enabled = true;
        }
        if (titleText != null)
        {
            titleText.gameObject.SetActive(true);
            titleText.enabled = true;
        }
        if (completeButton != null)
        {
            completeButton.gameObject.SetActive(true);
            completeButton.enabled = true;
        }
        if (completeButtonIcon != null)
        {
            completeButtonIcon.enabled = true;
        }

        _myHabitId = habitData.Id;
        _manager = manager;
        titleText.text = habitData.Title;

        // Item click (Background)
        itemButton.onClick.RemoveAllListeners();
        itemButton.onClick.AddListener(OnItemClicked);

        // Completion click
        completeButton.onClick.RemoveAllListeners();
        completeButton.onClick.AddListener(OnCompleteClicked);

        // Visual feedback if already done today
        completeButtonIcon.color = habitData.IsCompletedToday ? Color.green : Color.white;
    }
    private void OnCompleteClicked()
    {
        _manager.ToggleHabitCompletion(_myHabitId);
    }

    private void OnItemClicked()
    {
        // Tell the manager: "Hey, the player clicked ME. Here is my ID!"
        _manager.OpenHabitDetails(_myHabitId);
    }
}