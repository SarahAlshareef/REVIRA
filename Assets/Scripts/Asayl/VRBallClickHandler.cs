using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Oculus.Interaction;

public class VRBallClickHandler : MonoBehaviour
{
    [Header("UI References")]
    public GameObject productPopup;
    public GameObject previewButtonObject;
    public GameObject closeButtonObject;
    public GameObject specButtonObject;

    [Header("Product Settings")]
    public GameObject footballObject;

    [Header("Animation Settings")]
    public float moveDuration = 2.0f;

    [Header("References")]
    public PlayerControlManager controlManager;
    public GameObject specCanvas;

    private Transform vrCamera;

    private Rigidbody rb;
    private Grabbable grab;
    private AudioSource audioSource;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private bool scaleCaptured = false;

    private bool isPreviewing = false;
    private bool isHeld = false;

    public static VRBallClickHandler currentActiveHandler;

    [Header("Ball Interaction")]
    public float previewScale = 0.65f;
    public AudioClip bounceSound;
    public float hapticDuration = 0.1f;
    public float hapticAmplitude = 0.5f;

    void Start()
    {
        if (productPopup != null)
            productPopup.SetActive(false);

        if (footballObject != null)
        {
            originalPosition = footballObject.transform.position;
            originalRotation = footballObject.transform.rotation;

            rb = footballObject.GetComponent<Rigidbody>();
            grab = footballObject.GetComponent<Grabbable>();
            audioSource = footballObject.GetComponent<AudioSource>();
        }

        GameObject cam = GameObject.Find("CenterEyeAnchor");
        if (cam != null)
            vrCamera = cam.transform;

        DisableInteraction();
    }

    void Update()
    {
        if (grab != null && grab.enabled)
        {
            bool leftGrab = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch) > 0.8f;
            bool rightGrab = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch) > 0.8f;

            if ((leftGrab || rightGrab) && !isHeld)
            {
                isHeld = true;
                rb.isKinematic = true;

                OVRInput.Controller activeHand = leftGrab ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
                Transform handTransform = activeHand == OVRInput.Controller.LTouch ?
                    GameObject.Find("LeftHandAnchor").transform :
                    GameObject.Find("RightHandAnchor").transform;

                footballObject.transform.SetParent(handTransform);
            }
            else if (!leftGrab && !rightGrab && isHeld)
            {
                isHeld = false;
                rb.isKinematic = false;
                rb.useGravity = true;

                footballObject.transform.SetParent(null);
            }
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
            specBtn.onClick.AddListener(OnSpecButtonPressed);
        }

        if (closeButtonObject != null)
        {
            Button closeBtn = closeButtonObject.GetComponent<Button>();
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(ClosePreview);
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

    public void OnSpecButtonPressed()
    {
        if (currentActiveHandler == null || vrCamera == null) return;

        Vector3 popupOffset = vrCamera.right * 0.6f + vrCamera.forward * 0.3f;
        productPopup.transform.position = vrCamera.position + popupOffset;
        productPopup.transform.rotation = Quaternion.LookRotation(productPopup.transform.position - vrCamera.position);

        ProductIdentifie identifier = footballObject.GetComponent<ProductIdentifie>();
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

        ReturnBallToShelf();
        isPreviewing = false;
    }

    void StartPreview()
    {
        if (vrCamera == null) return;

        
        footballObject.transform.SetParent(null);

 
        if (!scaleCaptured)
        {
            originalScale = footballObject.transform.localScale;
            scaleCaptured = true;
        }

        Vector3 offset = vrCamera.right * 0.6f + vrCamera.forward * 0.3f;
        productPopup.transform.position = vrCamera.position + offset;
        productPopup.transform.rotation = Quaternion.LookRotation(productPopup.transform.position - vrCamera.position);

        Vector3 previewCenter = vrCamera.position + vrCamera.forward * 1.4f;
        StartCoroutine(MoveBallToCenter(footballObject, previewCenter));

        isPreviewing = true;
    }

    IEnumerator MoveBallToCenter(GameObject obj, Vector3 targetPosition)
    {
        float elapsed = 0f;
        Vector3 startPos = obj.transform.position;
        Quaternion startRot = originalRotation;
        Quaternion targetRot = originalRotation;

        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;
            obj.transform.position = Vector3.Lerp(startPos, targetPosition, t);
            obj.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        obj.transform.position = targetPosition;
        obj.transform.rotation = originalRotation;

        footballObject.transform.localScale = originalScale * previewScale;

        EnableInteraction();
    }

    public void ClosePreview()
    {
        if (specCanvas != null && specCanvas.activeSelf)
        {
            Debug.Log("Cannot close preview while spec canvas is open");
            return;
        }

        if (productPopup != null)
            productPopup.SetActive(false);

        ReturnBallToShelf();

        if (controlManager != null)
            controlManager.UnlockControls();

        isPreviewing = false;
    }

    void ReturnBallToShelf()
    {
        if (footballObject != null)
        {
            footballObject.transform.position = originalPosition;
            footballObject.transform.rotation = originalRotation;
            footballObject.transform.localScale = originalScale;
        }

        DisableInteraction();
    }

    void EnableInteraction()
    {
        grab.enabled = true;
        rb.isKinematic = false;
        rb.useGravity = true;
    }

    void DisableInteraction()
    {
        grab.enabled = false;
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (bounceSound != null && collision.relativeVelocity.magnitude > 0.5f)
        {
            if (audioSource != null)
                audioSource.PlayOneShot(bounceSound);

            SendHapticPulse(OVRInput.Controller.RTouch);
            SendHapticPulse(OVRInput.Controller.LTouch);
        }
    }

    void SendHapticPulse(OVRInput.Controller controller)
    {
        OVRInput.SetControllerVibration(1, hapticAmplitude, controller);
        Invoke(nameof(StopHaptics), hapticDuration);
    }

    void StopHaptics()
    {
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
    }
}
