using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Oculus.Interaction;

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
    public float moveSpeed = 1.0f;              // Speed to move product while grabbing

    [Header("References")]
    public PlayerControlManager controlManager; // Central manager for locking/unlocking movement

    private Transform vrCamera;                 // CenterEyeAnchor camera reference
    private Vector3 originalProductPosition;    // Original shelf position
    private Quaternion originalProductRotation; // Original shelf rotation
    private Vector3 originalScale;              // Original shelf scale

    private bool isPreviewing = false;

    private Vector3 previewCenter;              // Center of preview area in front of player

    private float currentScale;
    public float minScale = 0.5f;
    public float maxScale = 1.2f;

    public GameObject specCanvas;               // Reference to specification canvas to check if open

    public static VRProductClickHandler currentActiveHandler;

    void Start()
    {
        if (productPopup != null)
            productPopup.SetActive(false);

        if (productObject != null)
        {
            originalProductPosition = productObject.transform.position;
            originalProductRotation = productObject.transform.rotation;
            originalScale = productObject.transform.localScale;
            currentScale = 1f;
        }

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
            Vector2 rotateInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
            productObject.transform.Rotate(vrCamera.up, rotateInput.x * 360f * Time.deltaTime, Space.World);
            productObject.transform.Rotate(vrCamera.right, -rotateInput.y * 360f * Time.deltaTime, Space.World);

            float zoomInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick).y;
            currentScale += zoomInput * Time.deltaTime;
            currentScale = Mathf.Clamp(currentScale, minScale, maxScale);
            productObject.transform.localScale = originalScale * currentScale;
        }
    }

    public void ShowPopup()
    {
        if (vrCamera == null) return;

        currentActiveHandler = this;
        Vector3 offset = vrCamera.forward * 1.4f;
        productPopup.transform.position = vrCamera.position + offset;
        productPopup.transform.rotation = Quaternion.LookRotation(productPopup.transform.position - vrCamera.position);
        productPopup.SetActive(true);

        if (previewButtonObject != null)
        {
            Button previewBtn = previewButtonObject.GetComponent<Button>();
            previewBtn.onClick.RemoveAllListeners();
            previewBtn.onClick.AddListener(OnPreviewButtonPressed);
        }

        if (specButtonObject != null)
        {
            Button specBtn = specButtonObject.GetComponent<Button>();
            specBtn.onClick.RemoveAllListeners();
            specBtn.onClick.AddListener(OnPreviewSpecButtonClick);
        }

        if (closeButtonObject != null)
        {
            Button closeBtn = closeButtonObject.GetComponent<Button>();
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(ClosePreview);
            }
        }

        controlManager.LockControls();
    }

    public void OnPreviewButtonPressed()
    {
        if (specCanvas != null && specCanvas.activeSelf)
        {
            specCanvas.SetActive(false);
        }

        if (currentActiveHandler != null)
            currentActiveHandler.StartPreview();
    }

    public void OnPreviewSpecButtonClick()
    {
        if (currentActiveHandler == null) return;
        if (vrCamera == null) return;

        Vector3 popupOffset = vrCamera.right * 0.6f + vrCamera.forward * 0.3f;
        productPopup.transform.position = vrCamera.position + popupOffset;
        productPopup.transform.rotation = Quaternion.LookRotation(productPopup.transform.position - vrCamera.position);

        ProductIdentifie identifier = productObject.GetComponent<ProductIdentifie>();
        if (identifier == null) return;

        ProductsManager products = FindObjectOfType<ProductsManager>();
        if (products == null) return;

        products.storeID = identifier.StoreID;
        products.productID = identifier.ProductID;
        products.LoadProductData();

        if (products.productPopup != null)
        {
            Vector3 frontOffset = vrCamera.forward * 1.4f;
            products.productPopup.transform.position = vrCamera.position + frontOffset;
            products.productPopup.transform.rotation = Quaternion.LookRotation(products.productPopup.transform.position - vrCamera.position);
        }

        products.OnPreviewSpecificationClick();

        ReturnProductToShelf();
        isPreviewing = false;
    }

    void StartPreview()
    {
        if (vrCamera == null) return;

        Vector3 offset = vrCamera.right * 0.6f + vrCamera.forward * 0.3f;
        productPopup.transform.position = vrCamera.position + offset;
        productPopup.transform.rotation = Quaternion.LookRotation(productPopup.transform.position - vrCamera.position);

        previewCenter = vrCamera.position + vrCamera.forward * 1.4f;
        StartCoroutine(MoveProductToCenter(productObject, previewCenter));

        isPreviewing = true;
    }

    IEnumerator MoveProductToCenter(GameObject obj, Vector3 targetPosition)
    {
        float elapsed = 0f;
        Vector3 startPos = obj.transform.position;
        Quaternion startRot = originalProductRotation; // Start with original rotation
        Quaternion targetRot = originalProductRotation; // Maintain original rotation throughout

        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;
            obj.transform.position = Vector3.Lerp(startPos, targetPosition, t);
            obj.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        obj.transform.position = targetPosition;
        obj.transform.rotation = originalProductRotation;
        currentScale = 0.8f; // Smaller scale when previewing
        productObject.transform.localScale = originalScale * currentScale;
    }

    public void ClosePreview()
    {
        if (specCanvas != null && specCanvas.activeSelf)
        {
            Debug.Log("Cannot close preview while spec canvas is open");
            return;
        }

        if (productPopup != null)
        {
            productPopup.SetActive(false);
        }

        if (productObject != null)
        {
            productObject.transform.position = originalProductPosition;
            productObject.transform.rotation = originalProductRotation;
            productObject.transform.localScale = originalScale;
        }

        if (controlManager != null)
        {
            controlManager.UnlockControls();
        }

        isPreviewing = false;
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

