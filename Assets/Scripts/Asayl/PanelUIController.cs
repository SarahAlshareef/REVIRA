using UnityEngine;
using UnityEngine.UI;

public class PanelUIController : MonoBehaviour
{
    [Header("Panel Info")]
    public string panelName; // Optional name for identification

    [Header("UI Triggers (Optional)")]
    public Button openButton;  // UI Button to open the panel
    public Button closeButton; // UI Button to close the panel

    [Header("Panel References")]
    public GameObject overlayBackground; // The dark background overlay
    public GameObject interactivePanel;  // The actual interactive UI
    public MonoBehaviour movementScript; // Movement script to enable/disable

    [Header("Camera Settings")]
    public Transform cameraTransform; // Player's camera reference
    public float uiDistance = 2f;     // Distance in front of the camera

    [Header("Menu Button Activation")]
    public bool useMenuButton = false; // Enable if this panel opens via controller menu button

    void Start()
    {
        // Hide both UI elements at start
        overlayBackground?.SetActive(false);
        interactivePanel?.SetActive(false);

        // Assign UI button events if they exist
        if (openButton != null)
            openButton.onClick.AddListener(OpenUI);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseUI);
    }

    void Update()
    {
        // Listen for controller menu button if enabled
        if (useMenuButton && OVRInput.GetDown(OVRInput.Button.Start))
        {
            ToggleUI();
        }
    }

    public void OpenUI()
    {
        if (overlayBackground != null) overlayBackground.SetActive(true);
        if (interactivePanel != null) interactivePanel.SetActive(true);

        if (movementScript != null)
            movementScript.enabled = false;

        PositionPanelsInFrontOfCamera();
    }

    public void CloseUI()
    {
        if (overlayBackground != null) overlayBackground.SetActive(false);
        if (interactivePanel != null) interactivePanel.SetActive(false);

        if (movementScript != null)
            movementScript.enabled = true;
    }

    public void ToggleUI()
    {
        if (interactivePanel != null && interactivePanel.activeSelf)
            CloseUI();
        else
            OpenUI();
    }

    private void PositionPanelsInFrontOfCamera()
    {
        if (cameraTransform == null) return;

        Vector3 position = cameraTransform.position + cameraTransform.forward * uiDistance;
        Quaternion rotation = cameraTransform.rotation;

        if (overlayBackground != null)
            overlayBackground.transform.SetPositionAndRotation(position, rotation);

        if (interactivePanel != null)
            interactivePanel.transform.SetPositionAndRotation(position, rotation);
    }
}
