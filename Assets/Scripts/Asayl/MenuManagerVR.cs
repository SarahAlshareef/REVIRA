using UnityEngine;
using UnityEngine.UI;

public class MenuManagerVR : MonoBehaviour
{
    [Header("UI References")]
    public GameObject menuUI;
    public GameObject dimmerCanvas;

    [Header("Movement Control")]
    public PlayerControlManager playerControlManager;

    [Header("Input")]
    public OVRInput.Button menuButton = OVRInput.Button.Start;

    [Header("Camera")]
    public Transform targetCamera;

    [Header("External UI Panels")]
    public GameObject[] allManagedUIs; // Cart UI, Checkout UIs, etc.

    private bool menuIsOpen = false;
    private bool blockMenuToggle = false;

    void Start()
    {
        menuUI.SetActive(false);
        dimmerCanvas.SetActive(false);
    }

    void Update()
    {
        if (OVRInput.GetDown(menuButton) && !blockMenuToggle)
        {
            if (!menuIsOpen)
                OpenMenu();
            else
                CloseMenu();
        }

        if (dimmerCanvas.activeInHierarchy && targetCamera != null)
        {
            dimmerCanvas.transform.position = targetCamera.position;
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

    public void OnMenuButtonClicked()
    {
        blockMenuToggle = true;
        CloseMenu();

        if (!dimmerCanvas.activeInHierarchy)
        {
            dimmerCanvas.SetActive(true);
        }
    }

    public void OnCloseButtonClicked()
    {
        // Close all external UIs
        foreach (var ui in allManagedUIs)
        {
            if (ui.activeInHierarchy)
                ui.SetActive(false);
        }

        ResetSession();
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
        Vector3 flatForward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
        Vector3 targetPos = cam.position + flatForward * 2f;
        targetPos.y = cam.position.y + 0.9f; // Fixed height
        ui.transform.position = targetPos;
        ui.transform.rotation = Quaternion.LookRotation(flatForward);
    }
}
