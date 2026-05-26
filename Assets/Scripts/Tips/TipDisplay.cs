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
        StartCoroutine(InitWhenReady());
    }

    private IEnumerator InitWhenReady()
    {
        // Wait until TipDatabase has finished fetching from Supabase
        yield return new WaitUntil(() => TipDatabase.instance != null && TipDatabase.instance.IsReady());

        ShowNewTip();

        if (autoRotate)
            StartTipRotation();
    }

    void StartTipRotation()
    {
        if (tipRotationCoroutine != null)
            StopCoroutine(tipRotationCoroutine);

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
            tipText.text = TipDatabase.instance.GetRandomTip();
    }

    // Optional: manually trigger a new tip and reset the rotation timer
    public void ForceNewTip()
    {
        ShowNewTip();
        if (autoRotate)
            StartTipRotation();
    }
}