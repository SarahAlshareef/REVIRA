using UnityEngine;
using UnityEngine.UI;

public class MenuManagerVR : MonoBehaviour
{
    [Header("UI References")]
    public GameObject menuUI;
    public GameObject dimmerCanvas;

    [Header("Menu Buttons")]
    public Button[] menuButtons; // Buttons inside the menu that should block menu toggle

    [Header("Close Buttons (X)")]
    public Button[] closeButtons; // All X buttons from cart/checkout UIs

    [Header("External UI Panels")]
    public GameObject[] allManagedUIs; // All cart/checkout UIs to close together

    [Header("Movement Control")]
    public PlayerControlManager playerControlManager;

    [Header("Input")]
    public OVRInput.Button menuButton = OVRInput.Button.Start;

    [Header("Camera Follow (for dimmer)")]
    public Transform targetCamera;

    private bool menuIsOpen = false;
    private bool blockMenuToggle = false;
    private bool sessionActive = false;

    void Start()
    {
        // Menu internal buttons block menu toggle
        foreach (var btn in menuButtons)
        {
            btn.onClick.AddListener(() => blockMenuToggle = true);
        }

        // Close buttons (X) close all UIs and restore state
        foreach (var btn in closeButtons)
        {
            btn.onClick.AddListener(() =>
            {
                CloseAllManagedUIs();
                HandleUIClosed();
            });
        }

        menuUI.SetActive(false);
        dimmerCanvas.SetActive(false);
    }

    void Update()
    {
        // Toggle menu button (left controller)
        if (OVRInput.GetDown(menuButton) && !blockMenuToggle)
        {
            if (!menuIsOpen)
                OpenMenu();
            else
                CloseMenu();
        }

        // Move dimmer with camera
        if (dimmerCanvas.activeInHierarchy && targetCamera != null)
        {
            Vector3 pos = targetCamera.position + targetCamera.forward * 1.5f;
            pos.y = targetCamera.position.y;
            dimmerCanvas.transform.position = pos;
            dimmerCanvas.transform.rotation = targetCamera.rotation;
        }
    }

    public void OpenMenu()
    {
        sessionActive = true;
        menuIsOpen = true;

        menuUI.SetActive(true);
        dimmerCanvas.SetActive(true);
        playerControlManager.LockControls();

        FaceMenuToPlayer(menuUI); // use special centering for menu
    }

    public void CloseMenu()
    {
        menuIsOpen = false;
        menuUI.SetActive(false);
    }

    public void HandleUIOpened(GameObject ui)
    {
        if (menuIsOpen)
        {
            menuUI.SetActive(false);
            menuIsOpen = false;
        }

        sessionActive = true;
        blockMenuToggle = true;

        dimmerCanvas.SetActive(true);
        playerControlManager.LockControls();

        FaceUIToPlayer(ui);
    }

    public void HandleUIClosed()
    {
        menuIsOpen = false;

        if (!AnyOtherUIOpen())
        {
            sessionActive = false;
            dimmerCanvas.SetActive(false);
            playerControlManager.UnlockControls();
            blockMenuToggle = false;
        }
    }

    public void CloseAllManagedUIs()
    {
        foreach (GameObject ui in allManagedUIs)
        {
            if (ui.activeInHierarchy)
                ui.SetActive(false);
        }

        menuUI.SetActive(false);
        menuIsOpen = false;
    }

    bool AnyOtherUIOpen()
    {
        foreach (GameObject ui in allManagedUIs)
        {
            if (ui.activeInHierarchy)
                return true;
        }
        return false;
    }

    void FaceUIToPlayer(GameObject ui)
    {
        Transform cam = Camera.main.transform;

        // Position 3 meters forward + slightly above
        Vector3 targetPos = cam.position + cam.forward * 4f + cam.up * 0.6f;
        ui.transform.position = targetPos;

        // Rotate horizontally to face player
        Vector3 lookDir = new Vector3(cam.forward.x, 0, cam.forward.z);
        if (lookDir != Vector3.zero)
            ui.transform.rotation = Quaternion.LookRotation(lookDir);
    }

    void FaceMenuToPlayer(GameObject ui)
    {
        Transform cam = Camera.main.transform;

        // Appear exactly in the center of the player's view
        Vector3 targetPos = cam.position + cam.forward * 2.5f;
        ui.transform.position = targetPos;

        // Match camera rotation perfectly
        ui.transform.rotation = Quaternion.LookRotation(cam.forward, cam.up);
    }
}
