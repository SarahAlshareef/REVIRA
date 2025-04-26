using UnityEngine;
using UnityEngine.UI;

public class CartNavigationManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject cartCanvas;
    public GameObject promotionalCodeCanvas;

    [Header("UI Button")]
    public Button openCartButton;  
    public Button proceedToCheckoutButton;

    void Start()
    {
        if (openCartButton != null)
        {
            openCartButton.onClick.RemoveAllListeners();
            openCartButton.onClick.AddListener(ShowCart);

        }
        if (proceedToCheckoutButton != null)
        {
            proceedToCheckoutButton.onClick.RemoveAllListeners();
            proceedToCheckoutButton.onClick.AddListener(ProceedToPromo);
        }

        }

    public void ShowCart()
    {
        if (cartCanvas != null)
        {
            cartCanvas.SetActive(true);

            Transform cam = Camera.main.transform;
            Vector3 flatForward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
            Vector3 targetPos = cam.position + flatForward * 5f;
            targetPos.y = cam.position.y + 0.8f; // Fixed height
            cartCanvas.transform.position = targetPos;
            cartCanvas.transform.rotation = Quaternion.LookRotation(flatForward);
        }
    }

    public void ProceedToPromo()
    {
        if (cartCanvas != null)
            cartCanvas.SetActive(false);

        if (promotionalCodeCanvas != null)
        {
            promotionalCodeCanvas.SetActive(true);

            Transform cam = Camera.main.transform;
            Vector3 flatForward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
            Vector3 targetPos = cam.position + flatForward * 4f;
            targetPos.y = cam.position.y + 0.9f; // Fixed height
            promotionalCodeCanvas.transform.position = targetPos;
            promotionalCodeCanvas.transform.rotation = Quaternion.LookRotation(flatForward);

        }
    }
}

