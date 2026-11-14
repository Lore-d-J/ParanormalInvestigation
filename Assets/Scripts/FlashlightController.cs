
using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    public Light flashlight;
    private AudioSource flashlightAudioSource;

    void Awake()
    {
        if (transform.parent != null &&
            transform.parent.GetComponentInParent<FlashlightController>() != null)
        {
            enabled = false;
            return;
        }

        if (flashlight == null)
        {
            Debug.LogError("FlashlightController requires a Light reference.", this);
            enabled = false;
            return;
        }

        flashlightAudioSource = flashlight.GetComponent<AudioSource>();
        if (flashlightAudioSource != null)
        {
            flashlightAudioSource.playOnAwake = false;
            flashlightAudioSource.Stop();
        }
        else
        {
            Debug.LogWarning("No AudioSource found on the flashlight.", flashlight);
        }
    }


void Start()
    {
        if (flashlight != null)
        {
            flashlight.enabled = false;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            bool turnOn = !flashlight.enabled;
            flashlight.enabled = turnOn;

            if (turnOn && flashlightAudioSource != null)
            {
                flashlightAudioSource.Play();
            }
        }
    }
}
