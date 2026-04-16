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

    // This is called by the Manager when it spawns the button
    public void Setup(Habit habitData, HabitManager manager)
    {
        _myHabitId = habitData.Id;
        _manager = manager;

        titleText.text = habitData.Title;

        // Make the button click call the manager and pass its specific ID
        itemButton.onClick.RemoveAllListeners();
        itemButton.onClick.AddListener(OnItemClicked);
    }

    private void OnItemClicked()
    {
        // Tell the manager: "Hey, the player clicked ME. Here is my ID!"
        _manager.OpenHabitDetails(_myHabitId);
    }
}