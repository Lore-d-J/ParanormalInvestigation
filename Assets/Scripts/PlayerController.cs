
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f; //walkspeed
    public float jumpHeight = 1.5f; //jumpheight
    public float gravity = -9.81f; //pulls the character downward

    [Header("Mouse Settings")]
    public float mouseSensitivity = 100f;

    [Header("Camera")]
    public Transform playerCamera;

    private CharacterController controller;
    private Vector3 velocity;
    private float xRotation = 0f;

    void Awake()
    {
        controller = GetComponentInParent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("PlayerController requires a CharacterController on this object or a parent.", this);
            enabled = false;
            return;
        }

        PlayerController controllerOnPlayer = controller.GetComponent<PlayerController>();
        if (controllerOnPlayer != null && controllerOnPlayer != this)
        {
            enabled = false;
        }
    }

    void Start() //runs when the player starts
    {
        if (!enabled)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update() //continuously runs while the game is on
    {
        MovePlayer();
        LookAround();
    }

    void MovePlayer() //handles the walking and jumping
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        var playerTransform = controller.transform;
        Vector3 input = Vector3.ClampMagnitude(new Vector3(x, 0f, z), 1f);
        Vector3 move = playerTransform.right * input.x + playerTransform.forward * input.z;

        controller.Move(move * moveSpeed * Time.deltaTime);

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        if (Input.GetButtonDown("Jump") && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }

    void LookAround() //handles the camera movement
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        controller.transform.Rotate(Vector3.up * mouseX);
    }
}
