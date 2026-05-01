using UnityEngine;
using UnityEngine.EventSystems;

public class HomeScrollForwarder : MonoBehaviour, IDragHandler, IEndDragHandler
{
    [SerializeField] private BottomSheet bottomSheet;

    public void OnDrag(PointerEventData eventData)
    {
        bottomSheet.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        bottomSheet.OnEndDrag(eventData);
    }
}