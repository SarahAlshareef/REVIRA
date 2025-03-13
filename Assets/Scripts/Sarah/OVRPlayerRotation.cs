using UnityEngine;

public class OVRPlayerRotation : MonoBehaviour
{
    public float rotationSpeed = 50.0f; // Speed of rotation

    void Update()
    {
        // Get Right Joystick Input
        Vector2 rotationInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        // Rotate Player (Yaw - Left/Right)
        transform.Rotate(Vector3.up * rotationInput.x * rotationSpeed * Time.deltaTime);

        // Tilt Player (Pitch - Up/Down)
        transform.Rotate(Vector3.right * -rotationInput.y * rotationSpeed * Time.deltaTime);
    }
}
