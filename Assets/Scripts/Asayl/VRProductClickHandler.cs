
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class VRProductClickHandler : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public GameObject productPopup;             // Main popup UI (preview with buttons)
    public GameObject previewButtonObject;      // Button object for preview
    public GameObject closeButtonObject;        // Button object for closing preview
    public GameObject specButtonObject;         // Button object for product specification

    [Header("Product Settings")]
    public GameObject productObject;            // The actual product in the scene (not a clone)

    [Header("Animation Settings")]
    public float moveDuration = 1.0f;           // Time to animate product to center
    public float moveSpeed = 1.0f;              // Speed to move product while grabbing

    [Header("References")]
    public PlayerControlManager controlManager; // Central manager for locking/unlocking movement
    public Transform controllerTransform;       // Controller transform to track movement
    public Transform rayOrigin;                 // Controller ray origin (used for raycast)

    private Vector3 originalProductPosition;    // Original shelf position
    private Quaternion originalProductRotation; // Original shelf rotation

    private bool isPreviewing = false;
    private bool isGrabbing = false;

    private Vector3 previewCenter;              // Center of preview area in front of player
    private Vector3 previewAreaSize = new Vector3(0.5f, 0.5f, 0.5f);

    private float currentScale = 1f;
    public float minScale = 0.5f;
    public float maxScale = 3f;

    private Vector3 lastControllerPosition;

    void Start()
    {
        if (productPopup != null)
            productPopup.SetActive(false);

        // Save original position and rotation of the product
        if (productObject != null)
        {
            originalProductPosition = productObject.transform.position;
            originalProductRotation = productObject.transform.rotation;
        }
    }

    void Update()
    {
        HandleRaycastClick();

        if (isPreviewing && productObject != null)
        {
            // Grab input to allow manual movement inside limited space
            if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger) ||
                OVRInput.GetDown(OVRInput.Button.One) ||
                OVRInput.GetDown(OVRInput.Button.Three))
            {
                isGrabbing = true;
                lastControllerPosition = controllerTransform.position;
            }
            if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger) ||
                OVRInput.GetUp(OVRInput.Button.One) ||
                OVRInput.GetUp(OVRInput.Button.Three))
            {
                isGrabbing = false;
            }

            if (isGrabbing)
            {
                // Move product using controller delta position
                Vector3 delta = controllerTransform.position - lastControllerPosition;
                Vector3 targetPos = productObject.transform.position + delta * moveSpeed;
                productObject.transform.position = ClampPosition(targetPos);
                lastControllerPosition = controllerTransform.position;
            }

            // Rotate product with right thumbstick (free 360° rotation)
            float rotateInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x;
            productObject.transform.Rotate(Vector3.up, rotateInput * 360f * Time.deltaTime);

            // Zoom with left thumbstick
            float zoomInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;
            currentScale += zoomInput * Time.deltaTime;
            currentScale = Mathf.Clamp(currentScale, minScale, maxScale);
            productObject.transform.localScale = Vector3.one * currentScale;
        }
    }

    // Detect raycast from controller and handle click on product
    void HandleRaycastClick()
    {
        if (OVRInput.GetDown(OVRInput.Button.One) || OVRInput.GetDown(OVRInput.Button.Three))
        {
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    Debug.Log("[Raycast] Product clicked using controller ray");
                    ShowPopup();
                }
            }
        }
    }

    // Handles button clicks from UI
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.pointerPress == previewButtonObject)
        {
            StartPreview();
        }
        else if (eventData.pointerPress == closeButtonObject)
        {
            ClosePreview();
        }
        else if (eventData.pointerPress == specButtonObject)
        {
            ReturnProductToShelf();
        }
    }

    // Show UI popup and lock movement
    public void ShowPopup()
    {
        Vector3 pos = Camera.main.transform.position + Camera.main.transform.forward * 1.5f;
        productPopup.transform.position = pos;
        productPopup.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        productPopup.SetActive(true);

        controlManager.LockControls();
    }

    // Start preview by animating product to player view
    void StartPreview()
    {
        productPopup.transform.position = Camera.main.transform.position + Camera.main.transform.right * 1.2f;
        productPopup.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

        previewCenter = Camera.main.transform.position + Camera.main.transform.forward * 2f;

        StartCoroutine(MoveProductToCenter(productObject, previewCenter));

        isPreviewing = true;
    }

    // Move product to preview center
    IEnumerator MoveProductToCenter(GameObject obj, Vector3 targetPosition)
    {
        float elapsed = 0f;
        Vector3 startPos = obj.transform.position;
        Quaternion startRot = obj.transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(Camera.main.transform.forward);

        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;
            obj.transform.position = Vector3.Lerp(startPos, targetPosition, t);
            obj.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        obj.transform.position = targetPosition;
        obj.transform.rotation = targetRot;
    }

    // Limit movement inside preview area
    Vector3 ClampPosition(Vector3 pos)
    {
        Vector3 min = previewCenter - previewAreaSize;
        Vector3 max = previewCenter + previewAreaSize;
        return new Vector3(
            Mathf.Clamp(pos.x, min.x, max.x),
            Mathf.Clamp(pos.y, min.y, max.y),
            Mathf.Clamp(pos.z, min.z, max.z)
        );
    }

    // Close preview and reset everything
    public void ClosePreview()
    {
        productPopup.SetActive(false);
        ReturnProductToShelf();
        controlManager.UnlockControls();
        isPreviewing = false;
        isGrabbing = false;
    }

    // Return product to original shelf position without animation
    void ReturnProductToShelf()
    {
        if (productObject != null)
        {
            productObject.transform.position = originalProductPosition;
            productObject.transform.rotation = originalProductRotation;
        }
    }
}
