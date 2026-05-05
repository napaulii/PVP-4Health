using UnityEngine;

public class IslandRiser : MonoBehaviour
{
    [SerializeField] private BottomSheet bottomSheet;
    [SerializeField] private float riseAmount = 200f;

    private Vector3 basePosition;

    private void Start()
    {
        basePosition = transform.localPosition;
        bottomSheet.OnSheetMoved += HandleSheetMoved;
    }

    private void OnDestroy()
    {
        if (bottomSheet != null)
            bottomSheet.OnSheetMoved -= HandleSheetMoved;
    }

    private void HandleSheetMoved(float normalized)
    {
        transform.localPosition = basePosition + Vector3.up * (normalized * riseAmount);
    }
}