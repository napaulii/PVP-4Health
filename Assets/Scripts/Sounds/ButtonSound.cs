using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(() => {
            if (GlobalSound.Instance != null)
                GlobalSound.Instance.PlayButtonSound();
        });
    }
}