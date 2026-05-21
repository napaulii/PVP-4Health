using UnityEngine;
using UnityEngine.UI;

public class VolumeController : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;

    void Start()
    {
        if (volumeSlider != null)
        {
            // Nustato sliderio reikšmę
            if (GlobalSound.Instance != null)
            {
                volumeSlider.value = GlobalSound.Instance.GetVolume();
            }

            // Prideda listener'į
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
    }

    void OnVolumeChanged(float volume)
    {
        if (GlobalSound.Instance != null)
        {
            GlobalSound.Instance.SetVolume(volume);
        }
    }
}