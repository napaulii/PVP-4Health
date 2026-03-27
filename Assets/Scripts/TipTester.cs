// TipTester.cs - Compatible with both Input Systems
using UnityEngine;

public class TipTester : MonoBehaviour
{
    [ContextMenu("Show Random Tip")]
    void ShowRandomTip()
    {
        if (TipDatabase.instance != null)
        {
            string tip = TipDatabase.instance.GetRandomTip();
            Debug.Log("TEST TIP: " + tip);

            TipDisplay display = FindObjectOfType<TipDisplay>();
            if (display != null)
            {
                display.ShowNewTip();
            }
        }
        else
        {
            Debug.LogError("TipDatabase instance not found!");
        }
    }

    void Update()
    {
        // Use conditional compilation to handle both input systems
#if ENABLE_INPUT_SYSTEM
            // New Input System
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
                {
                    ShowRandomTip();
                }
            }
#else
        // Old Input System
        if (Input.GetKeyDown(KeyCode.T))
        {
            ShowRandomTip();
        }
#endif
    }
}