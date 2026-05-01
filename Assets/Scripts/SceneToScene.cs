using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    public string myScene;

    public void GoToScene()
    {
        SceneManager.LoadScene(myScene);
    }
}