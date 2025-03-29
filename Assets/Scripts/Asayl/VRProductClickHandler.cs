
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class VRProductClickHandler : MonoBehaviour
{
    [Header("UI References")]
    public GameObject productPopup;             // Main popup UI (preview with buttons)
    public GameObject previewButtonObject;      // Button object for preview
    public GameObject closeButtonObject;        // Button object for closing preview
    public GameObject specButtonObject;         // Button object for product specification

    [Header("Product Settings")]
    public GameObject productObject;            // The actual product in the scene (not a clone)

    [Header("Animation Settings")]
    public float moveDuration = 2.0f;           // Time to animate product to center
    public float moveSpeed = 1.5f;              // Speed to move product while grabbing

    [Header("References")]
    public PlayerControlManager controlManager; // Central manager for locking/unlocking movement
    public Transform controllerTransform;       // Controller transform to track movement
    public Transform rayOrigin;                 // Controller ray origin (used for raycast)

    private Transform vrCamera;                 // CenterEyeAnchor camera reference

    private Vector3 originalProductPosition;    // Original shelf position
    private Quaternion originalProductRotation; // Original shelf rotation
    private Vector3 originalScale;             // // Original shelf scale

    private bool isPreviewing = false;
    private bool isGrabbing = false;

    private Vector3 previewCenter;              // Center of preview area in front of player

    private float currentScale = 1f;
    public float minScale = 0.5f;
    public float maxScale = 1.5f;

    private Vector3 lastControllerPosition;

    // Static reference to track the active handler
    public static VRProductClickHandler currentActiveHandler;


    void Start()
    {
        if (productPopup != null)
            productPopup.SetActive(false);

        // Save original position and rotation of the product
        if (productObject != null)
        {
            originalProductPosition = productObject.transform.position;
            originalProductRotation = productObject.transform.rotation;
            originalScale = productObject.transform.localScale;
        }

        // Get reference to the VR camera inside OVR Camera Rig
        GameObject cam = GameObject.Find("CenterEyeAnchor");
        if (cam != null)
        {
            vrCamera = cam.transform;
        }
    }

    void Update()
    {
        if (isPreviewing && productObject != null)
        {
            if ((OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger) ||
                 OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger)) && !isGrabbing)
            {
                StartGrabbing();
            }
            // Detect grab release
            if (OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger) ||
                OVRInput.GetUp(OVRInput.Button.SecondaryHandTrigger))
            {
                isGrabbing = false;
            }

            if (isGrabbing)
            {
                Vector3 delta = controllerTransform.position - lastControllerPosition;
                Vector3 targetPos = productObject.transform.position + delta * moveSpeed;
                productObject.transform.position = targetPos; 
                lastControllerPosition = controllerTransform.position;
            }

            // Rotate 360° (left / right and up/ down)
            Vector2 rotateInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
            productObject.transform.Rotate(vrCamera.up, rotateInput.x * 360f * Time.deltaTime, Space.World);
            productObject.transform.Rotate(vrCamera.right, -rotateInput.y * 360f * Time.deltaTime, Space.World);


            // Zoom in/out
            float zoomInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;
            currentScale += zoomInput * Time.deltaTime;
            currentScale = Mathf.Clamp(currentScale, minScale, maxScale);
            productObject.transform.localScale = Vector3.one * currentScale;
        }
    }

    public void ShowPopup()
    {
        if (vrCamera == null) return;

        currentActiveHandler = this;

        Vector3 pos = vrCamera.position + vrCamera.forward * 1.5f;
        productPopup.transform.position = pos;
        productPopup.transform.rotation = Quaternion.LookRotation(vrCamera.forward);
        productPopup.SetActive(true);

        controlManager.LockControls();
    }


    public void OnPreviewButtonPressed()
    {
        if (currentActiveHandler != null)
        {
            currentActiveHandler.StartPreview();
        }
    }

    public void OnSpecButtonPressed()
    {
        if (currentActiveHandler != null)
        {
            currentActiveHandler.ReturnProductToShelf();
        }
    }

    void StartPreview()
    {  
            if (vrCamera == null) return;

        // Move UI to right and slightly closer to player
        Vector3 offset = vrCamera.right * 0.8f + vrCamera.forward * 0.3f;
        productPopup.transform.position = vrCamera.position + offset;

        // Rotate UI to face the player's view direction
        productPopup.transform.rotation = Quaternion.LookRotation(productPopup.transform.position - vrCamera.position);

        // Set the preview position
        previewCenter = vrCamera.position + vrCamera.forward * 2f;

        // Move product to center in front of the player
        StartCoroutine(MoveProductToCenter(productObject, previewCenter));
           
        controlManager.LockControls();

        currentScale = originalScale.x * 1.1f; // Slightly bigger than original
        productObject.transform.localScale = Vector3.one * currentScale;
  
        isPreviewing = true;
        StartGrabbing(); 
    }

    void StartGrabbing()
    {
        isGrabbing = true;
        lastControllerPosition = controllerTransform.position;
    }

    IEnumerator MoveProductToCenter(GameObject obj, Vector3 targetPosition)
    {
        float elapsed = 0f;
        Vector3 startPos = obj.transform.position;
        Quaternion startRot = obj.transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(targetPosition - vrCamera.position);

        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;
            obj.transform.position = Vector3.Lerp(startPos, targetPosition, t);
            obj.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        obj.transform.position = targetPosition;
        obj.transform.rotation = Quaternion.LookRotation(targetPosition - vrCamera.position);
    }

    public void ClosePreview()
    {
        productPopup.SetActive(false);
        ReturnProductToShelf();
        controlManager.UnlockControls();
        isPreviewing = false;
        isGrabbing = false;
    }

    void ReturnProductToShelf()
    {
        if (productObject != null)
        {
            productObject.transform.position = originalProductPosition;
            productObject.transform.rotation = originalProductRotation;
            productObject.transform.localScale = originalScale;
        }
    }
}

