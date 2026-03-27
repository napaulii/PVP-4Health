// TipDisplay.cs (Coroutine Version)
using UnityEngine;
using TMPro;
using System.Collections;

public class TipDisplay : MonoBehaviour
{
    public TextMeshProUGUI tipText;

    [Header("Auto-Rotation Settings")]
    public float tipChangeInterval = 10f; // Time in seconds between tip changes
    public bool autoRotate = true;

    private Coroutine tipRotationCoroutine;

    void Start()
    {
        // Make sure the database is ready before we try to show a tip
        if (TipDatabase.instance != null)
        {
            ShowNewTip();
        }

        // Start auto-rotation if enabled
        if (autoRotate)
        {
            StartTipRotation();
        }
    }

    void StartTipRotation()
    {
        if (tipRotationCoroutine != null)
        {
            StopCoroutine(tipRotationCoroutine);
        }
        tipRotationCoroutine = StartCoroutine(RotateTips());
    }

    IEnumerator RotateTips()
    {
        while (autoRotate)
        {
            yield return new WaitForSeconds(tipChangeInterval);
            ShowNewTip();
        }
    }

    public void ShowNewTip()
    {
        if (tipText != null && TipDatabase.instance != null)
        {
            // Get a random tip from the database and update the UI
            tipText.text = TipDatabase.instance.GetRandomTip();
        }
    }

    // Optional: Method to manually trigger a new tip
    public void ForceNewTip()
    {
        ShowNewTip();
        // Reset the coroutine timer
        if (autoRotate)
        {
            StartTipRotation();
        }
    }
}