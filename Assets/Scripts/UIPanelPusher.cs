using UnityEngine;

[ExecuteAlways] // Runs in Edit Mode too so you can preview it
public class UIPanelPusher : MonoBehaviour
{
    [Tooltip("Drag PersonalChallWin here")]
    public RectTransform personalPanel;

    [Tooltip("Drag GroupChallWin here")]
    public RectTransform groupPanel;

    [Tooltip("Spacing gap between the two panels")]
    public float spacing = 30f;

    private float _lastHeight;

    void Update()
    {
        if (personalPanel == null || groupPanel == null) return;

        // Read the current height of the expanding Personal Panel
        float currentHeight = personalPanel.rect.height;

        // Only update position if the height has actually changed (prevents GC allocation)
        if (Mathf.Abs(currentHeight - _lastHeight) > 0.01f)
        {
            _lastHeight = currentHeight;

            // Calculate the new Y position of the Group Panel directly below the Personal Panel
            Vector3 pos = groupPanel.anchoredPosition;
            pos.y = personalPanel.anchoredPosition.y - currentHeight - spacing;
            groupPanel.anchoredPosition = pos;
        }
    }
}