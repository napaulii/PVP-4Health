using UnityEngine;
using UnityEngine.UI;

public class GlobalToggle : MonoBehaviour
{
    public static event System.Action<bool> OnToggled;

    private void Awake()
    {
        GetComponent<Toggle>().onValueChanged.AddListener(val => OnToggled?.Invoke(val));
    }
}