using UnityEngine;

public class PreviewSystem : MonoBehaviour
{
    [SerializeField] private float previewYOffset = 0.06f;
    [SerializeField] private GameObject cellIndicator;
    [SerializeField] private Material previewMaterialPrefab;

    private GameObject previewObject;
    private Material previewMaterialInstance;
    private Renderer cellIndicatorRenderer;
    private Material cellIndicatorMaterialInstance;

    private void Start()
    {
        previewMaterialInstance = new Material(previewMaterialPrefab);

        cellIndicatorRenderer = cellIndicator.GetComponent<Renderer>();
        if (cellIndicatorRenderer == null)
            cellIndicatorRenderer = cellIndicator.GetComponentInChildren<Renderer>(true);
            cellIndicatorRenderer.material = cellIndicatorMaterialInstance;


        if (cellIndicatorRenderer == null)
            Debug.LogError("[PreviewSystem] No Renderer on CellIndicator!");

        cellIndicator.SetActive(false);
    }

    public void StartShowingPlacementPreview(GameObject prefab, Vector2Int size)
    {
        previewObject = Instantiate(prefab);
        PreparePreview(previewObject);
        PrepareCursor(size);
        cellIndicator.SetActive(true);
    }

    private void PrepareCursor(Vector2Int size)
    {
        if (size.x > 0 || size.y > 0)
        {
            cellIndicator.transform.localScale = new Vector3(size.x, 1, size.y);
            if (cellIndicatorMaterialInstance != null)
                cellIndicatorMaterialInstance.mainTextureScale = size;
        }
    }

    private void PreparePreview(GameObject previewObject)
    {
        Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();
        Debug.Log($"[Preview] Renderers found: {renderers.Length} on {previewObject.name}");

        foreach (Renderer renderer in renderers)
        {
            Debug.Log($"[Preview] Renderer: {renderer.gameObject.name}");
            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
                materials[i] = previewMaterialInstance;
            renderer.materials = materials;
        }
    }

    public void StopShowingPreview()
    {
        cellIndicator.SetActive(false);
        if (previewObject != null)
            Destroy(previewObject);
    }

    public void UpdatePosition(Vector3 position, bool validity)
    {
        if (previewObject != null)
        {
            MovePreview(position);
            ApplyFeedbackToPreview(validity);
        }
        MoveCursor(position);
        ApplyFeedbackToCursor(validity);
    }

    private void ApplyFeedbackToPreview(bool validity)
    {
        Color c = validity ? Color.white : Color.red;
        c.a = 0.5f;
        previewMaterialInstance.color = c;
    }

    private void ApplyFeedbackToCursor(bool validity)
    {
        Color c = validity ? Color.white : Color.red;
        c.a = 0.5f;
        if (cellIndicatorMaterialInstance != null)
            cellIndicatorMaterialInstance.color = c;
    }

    private void MoveCursor(Vector3 position)
    {
        cellIndicator.transform.position = position;
    }

    private void MovePreview(Vector3 position)
    {
        previewObject.transform.position = new Vector3(
            position.x,
            position.y + previewYOffset,
            position.z);
    }

    internal void StartShowingRemovePreview()
    {
        cellIndicator.SetActive(true);
        PrepareCursor(Vector2Int.one);
        ApplyFeedbackToCursor(false);
    }
}