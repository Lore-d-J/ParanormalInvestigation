
using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    public Light flashlight; //refer to the flashlight

    void Start()
    {
        flashlight.enabled = true;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F)) //detects if F is pressed
        {
            flashlight.enabled = !flashlight.enabled; //turn the flashlight on and off
        }
    }
}
