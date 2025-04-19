using UnityEngine;

public class OVRPlayerRotation : MonoBehaviour
{
    public float smoothRotationSpeed = 50.0f; // Speed for smooth rotation
    public float snapAngle = 30.0f; // Degrees for snap turn
    public float snapThreshold = 0.8f; // Joystick threshold for snap
    public float snapCooldown = 0.3f; // Cooldown between snaps

    public bool useSnapRotation = true; // Toggle between snap & smooth

    private float lastSnapTime = 0f;

    void Update()
    {
        Vector2 rotationInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        if (useSnapRotation)
        {
            HandleSnapRotation(rotationInput);
        }
        else
        {
            HandleSmoothRotation(rotationInput);
        }
    }

    void HandleSmoothRotation(Vector2 rotationInput)
    {
        // Rotate only on Y-axis (Yaw)
        transform.Rotate(0, rotationInput.x * smoothRotationSpeed * Time.deltaTime, 0);
    }

    void HandleSnapRotation(Vector2 rotationInput)
    {
        // Check cooldown
        if (Time.time - lastSnapTime < snapCooldown)
            return;

        if (rotationInput.x >= snapThreshold)
        {
            // Snap turn right
            transform.Rotate(0, snapAngle, 0);
            lastSnapTime = Time.time;
        }
        else if (rotationInput.x <= -snapThreshold)
        {
            // Snap turn left
            transform.Rotate(0, -snapAngle, 0);
            lastSnapTime = Time.time;
        }
    }
}
