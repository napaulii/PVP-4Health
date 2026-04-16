// TipController.cs
// Attach this script to the TipController GameObject in your scene.
// Wire up the references in the Inspector to: TipBanner, TipText, and CloseButton.

using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TipController : MonoBehaviour
{
    [Header("TipSystem References")]
    public RectTransform tipBanner;        // Drag TipBanner (Panel) here — NOTE: RectTransform, not GameObject
    public TextMeshProUGUI tipText;        // Drag TipText (TextMeshProUGUI) here
    public Button closeButton;             // Drag CloseButton here

    [Header("Animation Settings")]
    public float animationDuration = 0.4f; // Seconds for slide in/out
    public float bannerHeight = 200f;      // Match the height of your TipBanner RectTransform

    // The banner sits anchored to the top of the screen.
    // Hidden position: fully above the screen (positive Y offset pushes it above anchor).
    // Shown position: slid down just below the top edge (Y = 0).
    private Vector2 hiddenPosition;
    private Vector2 shownPosition;

    private Coroutine currentAnimation;

    void Start()
    {
        // In the Inspector, set TipBanner's RectTransform anchors to top-center:
        //   Anchor Min (0.5, 1), Anchor Max (0.5, 1), Pivot (0.5, 1)
        // This makes Y=0 sit flush at the top, and positive Y push it above/off-screen.

        shownPosition  = new Vector2(0f, 950f);                // Banner's resting position on screen
        hiddenPosition = new Vector2(0f, 1200f + bannerHeight); // Fully above the top edge

        // Start hidden, off-screen above
        tipBanner.anchoredPosition = hiddenPosition;

        closeButton.onClick.AddListener(HideTip);

        // Show a tip as soon as the scene loads
        ShowTip();
    }

    /// <summary>
    /// Fetches a random tip from TipDatabase and slides the TipBanner down.
    /// Call from any other script: FindObjectOfType<TipController>().ShowTip();
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
    /// Automatically called by CloseButton, or call manually: FindObjectOfType<TipController>().HideTip();
    /// </summary>
    public void HideTip()
    {
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(SlideTo(hiddenPosition));
    }

    /// <summary>
    /// Smoothly moves the TipBanner to the target anchored position using an ease-out curve.
    /// </summary>
    private IEnumerator SlideTo(Vector2 targetPosition)
    {
        Vector2 startPosition = tipBanner.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);

            // Ease-out cubic: feels snappy like iOS notifications
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            tipBanner.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, easedT);
            yield return null;
        }

        tipBanner.anchoredPosition = targetPosition;
    }
}