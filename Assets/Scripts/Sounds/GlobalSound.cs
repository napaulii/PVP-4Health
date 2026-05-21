using UnityEngine;
using UnityEngine.UI;

public class GlobalSound : MonoBehaviour
{
    public static GlobalSound Instance;

    [Header("Sound Settings")]
    [SerializeField] private AudioClip buttonClickSound;
    private AudioSource audioSource;

    private float currentVolume = 0.7f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = currentVolume;
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

    public void SetVolume(float volume)
    {
        currentVolume = volume;
        if (audioSource != null)
        {
            audioSource.volume = currentVolume;
        }
    }

    public float GetVolume()
    {
        return currentVolume;
    }
}