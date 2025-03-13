using UnityEngine;

public class OVRPlayerMovement : MonoBehaviour
{
    public float moveSpeed = 2.0f; // Speed of movement
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

        // Get Left Joystick Input
        Vector2 moveInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        moveDirection += transform.right * moveInput.x; // Strafe left/right
        moveDirection += transform.forward * moveInput.y; // Move forward/backward

        // Apply movement
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
    }
}
