using Unity.VisualScripting;
using UnityEngine;

public class FortressUpdateScript : MonoBehaviour
{
    public int thisLevel;
    public int nextLevel;
    [SerializeField] GameObject[] fortresses;
    private GameObject newFortress;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        LevelUp();
    }

    void LevelUp()
    {
        if(thisLevel == nextLevel)
        {
            newFortress = fortresses[thisLevel];
            
        }
    }
}
