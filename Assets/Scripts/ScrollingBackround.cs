using UnityEngine;
using System.Collections.Generic;

public class ScrollingBackground : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float scrollSpeed = 5f;
    [SerializeField] private float planeLength = 10f; // Set this to match your plane's length

    private GameObject originalPlane;
    private List<GameObject> planes = new List<GameObject>();

    void Start()
    {
        // Store this plane as the original
        originalPlane = gameObject;
        planes.Add(originalPlane);

        // Create 2-3 additional planes to start with
        for (int i = 1; i <= 3; i++)
        {
            CreateCopyAtPosition(originalPlane.transform.position.x + (planeLength * i));
        }
    }

    void Update()
    {
        // Move all planes left
        foreach (GameObject plane in planes)
        {
            if (plane != null)
                plane.transform.position += Vector3.left * scrollSpeed * Time.deltaTime;
        }

        // Check if we need to create a new plane on the right
        GameObject rightmostPlane = GetRightmostPlane();
        if (rightmostPlane != null && rightmostPlane.transform.position.x < planeLength * 2)
        {
            CreateCopyAtPosition(rightmostPlane.transform.position.x + planeLength);
        }

        // Remove planes that are too far left
        for (int i = planes.Count - 1; i >= 0; i--)
        {
            if (planes[i] != null && planes[i].transform.position.x < -planeLength * 2)
            {
                if (planes[i] != originalPlane) // Don't delete the original
                {
                    Destroy(planes[i]);
                    planes.RemoveAt(i);
                }
            }
        }
    }

    GameObject GetRightmostPlane()
    {
        if (planes.Count == 0) return null;

        GameObject rightmost = planes[0];
        for (int i = 1; i < planes.Count; i++)
        {
            if (planes[i] != null && planes[i].transform.position.x > rightmost.transform.position.x)
                rightmost = planes[i];
        }
        return rightmost;
    }

    void CreateCopyAtPosition(float xPos)
    {
        Vector3 newPos = new Vector3(xPos, transform.position.y, transform.position.z);
        GameObject newPlane = Instantiate(originalPlane, newPos, transform.rotation);
        planes.Add(newPlane);
    }
}