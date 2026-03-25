using UnityEngine;
using UnityEngine.SceneManagement;

public class LandingPage : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void LoadScene(string sceneName = "Home")
    {
        Debug.Log("Loading scene: " + sceneName);
        SceneManager.LoadScene(sceneName);
        
        // Optional: Destroy this GameObject after loading so it doesn't interfere with the new scene
        Destroy(gameObject); 
    }
}
