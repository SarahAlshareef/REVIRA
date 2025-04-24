using UnityEngine;
using UnityEngine.UI;

public class MenuManagerVR : MonoBehaviour
{
    [Header("UI References")]
    public GameObject menuUI;
    public GameObject dimmerCanvas;

    [Header("Menu Buttons")]
    public Button[] menuButtons; // Buttons inside the menu that block menu toggle

    [Header("X Buttons (Close)")]
    public Button[] closeButtons; // All X buttons across all UIs

    [Header("Movement Control")]
    public PlayerControlManager playerControlManager;

    [Header("Input")]
    public OVRInput.Button menuButton = OVRInput.Button.Start;

    [Header("Camera")]
    public Transform targetCamera;

    private bool menuIsOpen = false;
    private bool blockMenuToggle = false;

    void Start()
    {
        // Disable menu toggle when clicking any button in the menu
        foreach (var btn in menuButtons)
        {
            btn.onClick.AddListener(() => blockMenuToggle = true);
        }

        // Restore everything when clicking X buttons
        foreach (var btn in closeButtons)
        {
            btn.onClick.AddListener(() => ResetSession());
        }

        menuUI.SetActive(false);
        dimmerCanvas.SetActive(false);
    }

    void Update()
    {
        // Toggle menu with controller button
        if (OVRInput.GetDown(menuButton) && !blockMenuToggle)
        {
            if (!menuIsOpen)
                OpenMenu();
            else
                CloseMenu();
        }

        // Make dimmer follow camera
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
        menuIsOpen = true;

        menuUI.SetActive(true);
        dimmerCanvas.SetActive(true);
        playerControlManager.LockControls();

        FaceMenuToPlayer(menuUI);
    }

    public void CloseMenu()
    {
        menuIsOpen = false;
        menuUI.SetActive(false);
    }

    public void ResetSession()
    {
        menuIsOpen = false;
        blockMenuToggle = false;

        menuUI.SetActive(false);
        dimmerCanvas.SetActive(false);
        playerControlManager.UnlockControls();
    }

    void FaceMenuToPlayer(GameObject ui)
    {
        Transform cam = Camera.main.transform;
        Vector3 targetPos = cam.position + cam.forward * 2.5f;
        ui.transform.position = targetPos;
        ui.transform.rotation = Quaternion.LookRotation(cam.forward, cam.up);
    }
}
