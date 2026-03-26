using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(GridLayoutGroup))]
public class AdaptiveShopGrid : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private int columns = 3;
    [Tooltip("Cell height as a fraction of cell width. 1.4 = slightly taller than wide.")]
    [SerializeField] private float cellAspectRatio = 1.4f; 

    private GridLayoutGroup grid;
    private RectTransform rect;
    private float lastWidth = -1f;

    private void Awake()
    {
        grid = GetComponent<GridLayoutGroup>();
        rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
       
        Recalculate();
    }

    private void Update()
    {
        
        if (!Mathf.Approximately(rect.rect.width, lastWidth))
            Recalculate();
    }

    private void Recalculate()
    {
        lastWidth = rect.rect.width;
        if (lastWidth <= 0) return;

        float totalSpacing = grid.spacing.x * (columns - 1);
        float totalPadding = grid.padding.left + grid.padding.right;
        float cellWidth = (lastWidth - totalPadding - totalSpacing) / columns;
        float cellHeight = cellWidth * cellAspectRatio;

        grid.cellSize = new Vector2(cellWidth, cellHeight);
    }
}