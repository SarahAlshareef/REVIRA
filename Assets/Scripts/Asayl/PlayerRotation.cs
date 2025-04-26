using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using UnityEngine.EventSystems;

public class PlayerRotation : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float smoothRotationSpeed = 50.0f;
    public float snapAngle = 30.0f;
    public float snapThreshold = 0.8f;
    public float snapCooldown = 0.3f;
    public bool useSnapRotation = true;
    private float lastSnapTime = 0f;

    [Header("UI Elements")]
    public GameObject rotationPopupPanel;
    public Button snapButton;
    public Button smoothButton;
    public Button closeButton;
    public TextMeshProUGUI statusText;
    public Button reopenPopupButton;

    void Start()
    {
        if (rotationPopupPanel != null)
            rotationPopupPanel.SetActive(false); 

        if (snapButton != null)
            snapButton.onClick.AddListener(SetSnapRotation);

        if (smoothButton != null)
            smoothButton.onClick.AddListener(SetSmoothRotation);

        if (reopenPopupButton != null)
            reopenPopupButton.onClick.AddListener(ShowPopup);

        if (closeButton != null)
            closeButton.onClick.AddListener(HidePopup);

        UpdateStatusText();
    }

    void Update()
    {
        Vector2 rotationInput = OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);

        if (useSnapRotation)
        {
            HandleSnapRotation(rotationInput);
        }
        else
        {
            HandleSmoothRotation(rotationInput);
        }
    }

    void HandleSmoothRotation(Vector2 rotationInput)
    {
        transform.Rotate(0, rotationInput.x * smoothRotationSpeed * Time.deltaTime, 0);
    }

    void HandleSnapRotation(Vector2 rotationInput)
    {
        if (Time.time - lastSnapTime < snapCooldown)
            return;

        if (rotationInput.x >= snapThreshold)
        {
            transform.Rotate(0, snapAngle, 0);
            lastSnapTime = Time.time;
        }
        else if (rotationInput.x <= -snapThreshold)
        {
            transform.Rotate(0, -snapAngle, 0);
            lastSnapTime = Time.time;
        }
    }

    void SetSnapRotation()
    {
        useSnapRotation = true;
        UpdateStatusText();
    }

    void SetSmoothRotation()
    {
        useSnapRotation = false;
        UpdateStatusText();
    }

    void UpdateStatusText()
    {
        if (statusText != null)
        {
            statusText.text = useSnapRotation ? "Snap Rotation is currently active" : "Smooth Rotation is currently active";
        }
    }

    void HidePopup()
    {
        if (rotationPopupPanel != null)
            rotationPopupPanel.SetActive(false);
    }

    void ShowPopup()
    {
        if (rotationPopupPanel != null)
            rotationPopupPanel.SetActive(true);
    }

    public void ShowRotationPopup()
    {
        StartCoroutine(ShowRotationPopupCoroutine());
    }

    private IEnumerator ShowRotationPopupCoroutine()
    {
        if (rotationPopupPanel != null)
        {
            rotationPopupPanel.SetActive(true);

            yield return null;

            Transform cam = Camera.main.transform;
            Vector3 flatForward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
            Vector3 targetPos = cam.position + flatForward * 3.0f;
            targetPos.y = cam.position.y + 0.7f; // Fixed height
            rotationPopupPanel.transform.position = targetPos;
            rotationPopupPanel.transform.rotation = Quaternion.LookRotation(flatForward);
        }
    }

}
