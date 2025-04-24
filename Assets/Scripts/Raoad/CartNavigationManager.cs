
using UnityEngine;
public class CartNavigationManager : MonoBehaviour 
{

    [Header("UI Panels")]
    public GameObject cartCanvas;                
    public GameObject promotionalCodeCanvas;     

    public void ShowCart()
    {
        if (cartCanvas != null)
            cartCanvas.SetActive(true);

        Transform cam = Camera.main.transform;
        Vector3 targetPos = cam.position + cam.forward * 4.0f;

        cartCanvas.transform.position = targetPos;
        cartCanvas.transform.rotation = Quaternion.LookRotation(cam.forward, cam.up);
    }
    public void CloseCart()
    {
        if (cartCanvas != null)
            cartCanvas.SetActive(false);
    }
    public void ProceedToPromo()
    {
        if (cartCanvas != null)
            cartCanvas.SetActive(false);

        if (promotionalCodeCanvas != null)
            promotionalCodeCanvas.SetActive(true);

        Transform cam = Camera.main.transform;
        Vector3 targetPos = cam.position + cam.forward * 4.0f;

        cartCanvas.transform.position = targetPos;
        cartCanvas.transform.rotation = Quaternion.LookRotation(cam.forward, cam.up);
    }

   
}
