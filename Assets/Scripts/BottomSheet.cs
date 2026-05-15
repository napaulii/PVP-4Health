using UnityEngine;
using UnityEngine.EventSystems;

public class BottomSheet : MonoBehaviour, IDragHandler, IEndDragHandler
{
    private RectTransform rect;
    private Canvas canvas;

    [SerializeField] private float peekAmount = 150f;
    [SerializeField] private float topPadding = 60f;

    private float minY;
    private float maxY;

    public System.Action<float> OnSheetMoved; // 0 = closed, 1 = fully open

    void Start()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        float canvasHeight = canvas.GetComponent<RectTransform>().rect.height;
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, canvasHeight);

        minY = -(canvasHeight - peekAmount);
        maxY = topPadding;

        rect.anchoredPosition = new Vector2(0, minY);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 delta = eventData.delta / canvas.scaleFactor;
        float newY = Mathf.Clamp(rect.anchoredPosition.y + delta.y, minY, maxY);
        rect.anchoredPosition = new Vector2(0, newY);
        OnSheetMoved?.Invoke(GetNormalized());
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float mid = (minY + maxY) / 2f;
        float targetY = rect.anchoredPosition.y > mid ? maxY : minY;
        StartCoroutine(AnimateTo(targetY));
    }

    public void Toggle()
    {
        float mid = (minY + maxY) / 2f;
        float targetY = rect.anchoredPosition.y < mid ? maxY : minY;
        StartCoroutine(AnimateTo(targetY));
    }

    private float GetNormalized()
    {
        return Mathf.InverseLerp(minY, maxY, rect.anchoredPosition.y);
    }

    System.Collections.IEnumerator AnimateTo(float targetY)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        float startY = rect.anchoredPosition.y;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - Mathf.Pow(1f - elapsed / duration, 3f);
            rect.anchoredPosition = new Vector2(0, Mathf.Lerp(startY, targetY, t));
            OnSheetMoved?.Invoke(GetNormalized());
            yield return null;
        }

        rect.anchoredPosition = new Vector2(0, targetY);
        OnSheetMoved?.Invoke(GetNormalized());
    }
}