// TipController.cs
// Attach to the TipController GameObject in your scene.
// Wire up TipBanner (RectTransform), TipText, and CloseButton in the Inspector.
//
// HIERARCHY:
//   TipSystem        <- enabled
//     TipDatabase    <- enabled
//     TipController  <- disable THIS to suppress tips entirely
//       TipBanner    <- keep enabled (script controls it via position)
//         TipText    <- enabled
//         CloseButton<- enabled

using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TipController : MonoBehaviour
{
    [Header("TipSystem References")]
    public RectTransform tipBanner;         // Drag TipBanner (Panel) here
    public TextMeshProUGUI tipText;         // Drag TipText here
    public Button closeButton;              // Drag CloseButton here

    [Header("Animation Settings")]
    public float animationDuration = 0.4f;
    public float bannerHeight = 260f;       // Must match TipBanner's Height in RectTransform

    [Header("Push Notification Settings")]
    [Tooltip("How many seconds after app launch to send the push notification (e.g. 86400 = 24 hours)")]
    //  public int notificationDelaySeconds = 86400;
    public int notificationDelaySeconds = 10; // for testing, set to 10 seconds

    private Vector2 shownPosition;
    private Vector2 hiddenPosition;
    private Coroutine currentAnimation;

    void Start()
    {
        shownPosition = new Vector2(0f, 1050);
        hiddenPosition = new Vector2(0f, 12000f + bannerHeight);

        tipBanner.anchoredPosition = hiddenPosition;
        closeButton.onClick.AddListener(HideTip);

        // Wait for DB before showing tip or scheduling notification
        StartCoroutine(InitWhenReady());
    }

    private IEnumerator InitWhenReady()
    {
        // Wait until TipDatabase has finished fetching from Supabase
        yield return new WaitUntil(() => TipDatabase.instance != null && TipDatabase.instance.IsReady());

        ShowTip();
        SchedulePushNotification();
    }

    /// <summary>
    /// Shows the in-app TipBanner with a slide-down animation.
    /// Call from any script: FindObjectOfType<TipController>().ShowTip();
    /// </summary>
    public void ShowTip()
    {
        string tip = TipDatabase.instance.GetRandomTip();

        if (!string.IsNullOrEmpty(tip))
        {
            tipText.text = tip;

            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(SlideTo(shownPosition));
        }
    }

    /// <summary>
    /// Slides the TipBanner back up off-screen.
    /// Called by CloseButton, or manually: FindObjectOfType<TipController>().HideTip();
    /// </summary>
    public void HideTip()
    {
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(SlideTo(hiddenPosition));
    }

    /// <summary>
    /// Schedules an Android push notification using a random tip.
    /// Fires after notificationDelaySeconds (default: 24 hours after app launch).
    /// </summary>
    private void SchedulePushNotification()
    {
        if (NotificationManager.instance == null)
        {
            Debug.LogWarning("[TipController] NotificationManager not found in scene. Skipping push notification.");
            return;
        }

        string tip = TipDatabase.instance.GetRandomTip();

        if (!string.IsNullOrEmpty(tip))
        {
            NotificationManager.instance.ScheduleNotification(
                title: "Here's your daily tip!",
                body: tip,
                secondsFromNow: notificationDelaySeconds
            );
        }
    }

    private IEnumerator SlideTo(Vector2 targetPosition)
    {
        Vector2 startPosition = tipBanner.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);

            // Ease-out cubic — snappy like iOS notifications
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            tipBanner.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, easedT);
            yield return null;
        }

        tipBanner.anchoredPosition = targetPosition;
    }
}