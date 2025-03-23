
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class VRProductClickHandler : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    public GameObject productPopup;             // The main popup UI panel
    public GameObject previewButtonObject;      // The preview button object (with collider)
    public GameObject closeButtonObject;        // The close button object (with collider)

    [Header("Product Settings")]
    public GameObject productModel;             // The product prefab to display

    [Header("Animation Settings")]
    public float moveDuration = 1.0f;           // Time for product to animate to center

    private GameObject currentProductInstance;  // Active previewed product
    private bool isPreviewing = false;
    private bool isGrabbing = false;

    // Movement/Rotation scripts to disable during preview
    private OVRPlayerMovement playerMovement;
    private PlayerRotation playerRotation;

    // For movement limits
    private Vector3 previewCenter;
    private Vector3 previewAreaSize = new Vector3(0.5f, 0.5f, 0.5f); // Adjustable area around player

    private float currentScale = 1f;
    public float minScale = 0.5f;
    public float maxScale = 3f;

    void Start()
    {
        if (productPopup != null)
            productPopup.SetActive(false);

        // Get movement scripts from OVRCameraRig
        GameObject rig = GameObject.Find("OVRCameraRig");
        if (rig != null)
        {
            playerMovement = rig.GetComponent<OVRPlayerMovement>();
            playerRotation = rig.GetComponent<PlayerRotation>();
        }
    }

    void Update()
    {
        // Step 1: Grabbing logic with Trigger or A/X
        if (isPreviewing && currentProductInstance != null)
        {
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) ||
                OVRInput.GetDown(OVRInput.Button.One) ||
                OVRInput.GetDown(OVRInput.Button.Three))
            {
                isGrabbing = true;
            }
            if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger) ||
                OVRInput.GetUp(OVRInput.Button.One) ||
                OVRInput.GetUp(OVRInput.Button.Three))
            {
                isGrabbing = false;
            }

            // Step 2: While grabbing, move within defined bounds using Thumbstick
            if (isGrabbing)
            {
                Vector2 input = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
                Vector3 move = new Vector3(input.x, 0, input.y) * Time.deltaTime;
                Vector3 targetPos = currentProductInstance.transform.position + move;

                // Clamp movement to preview area
                targetPos = ClampPosition(targetPos);
                currentProductInstance.transform.position = targetPos;
            }

            // Step 3: Allow 360 rotation and zoom
            float rotateInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).x;
            currentProductInstance.transform.Rotate(Vector3.up, rotateInput * 100f * Time.deltaTime);

            float zoomInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;
            currentScale += zoomInput * Time.deltaTime;
            currentScale = Mathf.Clamp(currentScale, minScale, maxScale);
            currentProductInstance.transform.localScale = Vector3.one * currentScale;
        }
    }

    // Show popup in front of the player (called from XR Interactable)
    public void ShowPopup()
    {
        Vector3 pos = Camera.main.transform.position + Camera.main.transform.forward * 1.5f;
        productPopup.transform.position = pos;
        productPopup.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        productPopup.SetActive(true);

        DisablePlayerControls();
    }

    // This function is called manually via OnPointerClick from the preview button object
    public void StartPreviewFromUIButton()
    {
        StartPreview();
    }

    // Start preview mode with animated movement
    void StartPreview()
    {
        // Move popup to the side
        Vector3 side = Camera.main.transform.position + Camera.main.transform.right * 1.2f;
        productPopup.transform.position = side;
        productPopup.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

        // Set center of preview area
        previewCenter = Camera.main.transform.position + Camera.main.transform.forward * 2f;

        // Instantiate product at shelf (current position), animate to front of player
        currentProductInstance = Instantiate(productModel, transform.position, Quaternion.identity);
        currentProductInstance.transform.localScale = Vector3.one;
        StartCoroutine(MoveProductToCenter(currentProductInstance, previewCenter));

        isPreviewing = true;
    }

    // Smooth animation from shelf to front of player
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

    // Clamp movement to stay within preview area bounds
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

    // Disable walking and rotating scripts
    void DisablePlayerControls()
    {
        if (playerMovement != null) playerMovement.enabled = false;
        if (playerRotation != null) playerRotation.enabled = false;
    }

    // Re-enable walking and rotation
    public void ClosePreview()
    {
        if (currentProductInstance != null)
            Destroy(currentProductInstance);

        if (productPopup != null)
            productPopup.SetActive(false);

        if (playerMovement != null) playerMovement.enabled = true;
        if (playerRotation != null) playerRotation.enabled = true;

        isPreviewing = false;
        isGrabbing = false;
    }

    // Handle preview button clicks using Pointer Events
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
    }
}

