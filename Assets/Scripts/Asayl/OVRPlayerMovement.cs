using UnityEngine;

public class OVRPlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3.0f;

    private CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (characterController == null) return;

        // Get input from the left thumbstick (X: left/right, Y: forward/backward)
        Vector2 moveInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        // Convert input into world direction relative to player orientation
        Vector3 direction = (transform.right * moveInput.x + transform.forward * moveInput.y);

        // Apply movement speed and deltaTime
        Vector3 velocity = direction * moveSpeed * Time.deltaTime;

        // Apply gravity if not grounded
        if (!characterController.isGrounded)
        {
            velocity.y -= 9.81f * Time.deltaTime;
        }

        // Move the player
        characterController.Move(velocity);
    }
}
