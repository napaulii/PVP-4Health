using UnityEngine;
using UnityEngine.UI;

public class GlobalSound : MonoBehaviour
{
    public static GlobalSound Instance;

    [Header("Sound Settings")]
    [SerializeField] private AudioClip buttonClickSound;
    private AudioSource audioSource;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.7f;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }
}