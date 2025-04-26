// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class ExitStore : MonoBehaviour
{
    public GameObject popupPanel;
    public Button confirmButton, cancelButton;

    void Start()
    {
        popupPanel?.SetActive(false);

        confirmButton?.onClick.AddListener(ConfirmExit);
        cancelButton?.onClick.AddListener(ClosePopup);
    }

    public void ShowExitPopup()
    {
            popupPanel.SetActive(true);


        Transform cam = Camera.main.transform;
        Vector3 flatForward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
        Vector3 targetPos = cam.position + flatForward * 3.0f;
        targetPos.y = cam.position.y + 4.0f; // Fixed height
        popupPanel.transform.position = targetPos;
        popupPanel.transform.rotation = Quaternion.LookRotation(flatForward);
    }

    public void ConfirmExit()
    {
        popupPanel?.SetActive(false);
        SceneManager.LoadScene("StoreSelection");
    }

    public void ClosePopup()
    {
        popupPanel?.SetActive(false);
    }
}