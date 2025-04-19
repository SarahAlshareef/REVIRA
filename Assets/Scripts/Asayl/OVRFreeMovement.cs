using UnityEngine;

public class OVRFreeMovement : MonoBehaviour
{
    public float moveSpeed = 2.0f;
    public float rotationSpeed = 50.0f;
    private CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.center = new Vector3(0, 0.9f, 0);
            characterController.radius = 0.3f;
        }
    }

    void Update()
    {
        Vector3 moveDirection = Vector3.zero;

        // Left joystick controls full 360° movement
        Vector2 moveInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        moveDirection += transform.right * moveInput.x; // Move left/right
        moveDirection += transform.forward * moveInput.y; // Move forward/backward

        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

        // Right joystick controls free 360° rotation
        Vector2 rotationInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
        transform.Rotate(Vector3.up * rotationInput.x * rotationSpeed * Time.deltaTime);
        transform.Rotate(Vector3.right * -rotationInput.y * rotationSpeed * Time.deltaTime);
    }
}
