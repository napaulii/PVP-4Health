using UnityEngine;

public class ScrollingPlane : MonoBehaviour
{
    [SerializeField] private float speed = 5f;
    [SerializeField] private float planeLength = 10f;

    void Update()
    {
        // Move left
        transform.position += Vector3.left * speed * Time.deltaTime;

        // If completely off screen, move to the right
        if (transform.position.x <= -planeLength)
        {
            transform.position += new Vector3(planeLength * 2f, 0, 0);
        }
    }
}