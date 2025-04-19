using UnityEngine;

public class OVRPlayerMovement : MonoBehaviour
{
    public float moveSpeed = 2.0f;
    private CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (characterController == null) return;

        Vector3 moveDirection = Vector3.zero;

        // Get Left Joystick Input
        Vector2 moveInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        moveDirection += transform.right * moveInput.x; // Strafe left/right
        moveDirection += transform.forward * moveInput.y; // Move forward/backward

        // Apply Gravity (Prevents Floating Issues)
        if (!characterController.isGrounded)
        {
            moveDirection.y -= 9.81f * Time.deltaTime; // Simulate gravity
        }

        // Apply Movement With Collision Detection
        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
    }
}
