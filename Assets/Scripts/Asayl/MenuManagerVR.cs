using UnityEngine;
using UnityEngine.UI;

public class MenuManagerVR : MonoBehaviour
{
    [Header("UI References")]
    public GameObject menuUI;                  // Main menu UI panel
    public GameObject dimmerCanvas;            // Background dimmer canvas
    public GameObject[] checkoutUIs;           // All other UIs (cart + checkout screens)

    [Header("Buttons")]
    public Button[] closeButtons;              // All close (X) buttons

    [Header("Movement Control")]
    public PlayerControlManager playerControlManager; // Handles movement locking

    [Header("Input")]
    public OVRInput.Button menuButton = OVRInput.Button.Start; // Left controller menu button

    [Header("Camera Follow (for dimmer)")]
    public bool dimmerFollowsCamera = true;    // Enable/disable dimmer following camera
    public Transform targetCamera;             // Usually CenterEyeAnchor or Main Camera

    private bool menuIsOpen = false;
    private bool sessionActive = false;
    private GameObject lastHandledUI = null;

    void Start()
    {
        // Link all X buttons to close the session
        foreach (var btn in closeButtons)
            btn.onClick.AddListener(CloseAll);

        // Hide everything at start
        HideAllUI();
    }

    void Update()
    {
        // Handle menu button press
        if (OVRInput.GetDown(menuButton))
        {
            if (!sessionActive)
                OpenMenuSession(); // Start interaction session
            else if (menuIsOpen)
                CloseAll(); // Close menu only
        }

        // Monitor all external UIs
        foreach (GameObject ui in checkoutUIs)
        {
            if (ui.activeInHierarchy)
            {
                // First time seeing this UI activated
                if (lastHandledUI != ui)
                {
                    lastHandledUI = ui;

                    // Hide the menu if it's still open
                    if (menuIsOpen)
                    {
                        menuIsOpen = false;
                        menuUI.SetActive(false);
                    }

                    // If not already in session, enable dimmer & lock movement
                    if (!sessionActive)
                    {
                        sessionActive = true;
                        dimmerCanvas.SetActive(true);
                        playerControlManager.LockControls();
                    }

                    // Position the UI in front of the player
                    FaceUIToPlayer(ui);
                }
            }
        }

        // End session if all external UIs are closed
        if (sessionActive && AllCheckoutUIsClosed())
        {
            EndSession();
        }

        // Update dimmer position to follow the player
        if (dimmerFollowsCamera && dimmerCanvas.activeInHierarchy && targetCamera != null)
        {
            Vector3 followPos = targetCamera.position + targetCamera.forward * 1.5f;
            followPos.y = targetCamera.position.y;
            dimmerCanvas.transform.position = followPos;
            dimmerCanvas.transform.rotation = targetCamera.rotation;
        }
    }

    void OpenMenuSession()
    {
        sessionActive = true;
        menuIsOpen = true;

        menuUI.SetActive(true);
        FaceUIToPlayer(menuUI);

        dimmerCanvas.SetActive(true);
        playerControlManager.LockControls();
    }

    public void CloseAll()
    {
        menuIsOpen = false;
        menuUI.SetActive(false);
    }

    void EndSession()
    {
        sessionActive = false;
        menuIsOpen = false;
        lastHandledUI = null;

        dimmerCanvas.SetActive(false);
        playerControlManager.UnlockControls();
    }

    void HideAllUI()
    {
        menuUI.SetActive(false);
        foreach (GameObject ui in checkoutUIs)
            ui.SetActive(false);
    }

    bool AllCheckoutUIsClosed()
    {
        foreach (GameObject ui in checkoutUIs)
        {
            if (ui.activeInHierarchy)
                return false;
        }
        return true;
    }

    void FaceUIToPlayer(GameObject ui)
    {
        Transform cam = Camera.main.transform;

        // Show the UI 2 meters forward and slightly above the player
        Vector3 targetPos = cam.position + cam.forward * 2f + cam.up * 0.6f;
        ui.transform.position = targetPos;

        // Make the UI face the player horizontally
        Vector3 lookDir = new Vector3(cam.forward.x, 0, cam.forward.z);
        if (lookDir != Vector3.zero)
            ui.transform.rotation = Quaternion.LookRotation(lookDir);
    }
}

