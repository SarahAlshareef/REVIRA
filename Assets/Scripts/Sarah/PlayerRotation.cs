using UnityEngine;
using UnityEngine.UI; // Needed for UI elements
using TMPro;
using UnityEngine.EventSystems;

public class PlayerRotation : MonoBehaviour, IPointerClickHandler
{
    public float smoothRotationSpeed = 50.0f;
    public float snapAngle = 30.0f;
    public float snapThreshold = 0.8f;
    public float snapCooldown = 0.3f;

    public bool useSnapRotation = true;

    private float lastSnapTime = 0f;

    // UI Button reference (drag & drop in Inspector)
    public Button toggleButton;
    public TextMeshProUGUI toggleButtonText;

    void Start()
    {
        // Add listener to button
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleRotationMode);
            UpdateButtonText();
        }
    }

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
        transform.Rotate(0, rotationInput.x * smoothRotationSpeed * Time.deltaTime, 0);
    }

    void HandleSnapRotation(Vector2 rotationInput)
    {
        if (Time.time - lastSnapTime < snapCooldown)
            return;

        if (rotationInput.x >= snapThreshold)
        {
            transform.Rotate(0, snapAngle, 0);
            lastSnapTime = Time.time;
        }
        else if (rotationInput.x <= -snapThreshold)
        {
            transform.Rotate(0, -snapAngle, 0);
            lastSnapTime = Time.time;
        }
    }

    // Toggle Function for Button
    public void ToggleRotationMode()
    {
        useSnapRotation = !useSnapRotation;
        UpdateButtonText();
    }

    // Update Button Text
    private void UpdateButtonText()
    {
        if (toggleButtonText != null)
        {
            toggleButtonText.text = useSnapRotation ? "Snap: ON" : "Snap: OFF";
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ToggleRotationMode();
    }
}
