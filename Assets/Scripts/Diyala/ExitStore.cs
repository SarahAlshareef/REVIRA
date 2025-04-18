// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class ExitStore : MonoBehaviour
{
    public GameObject popupPanel;
    public Button confirmButton, cancelButton;
    public Transform playerCamera;
    public float popupDistance = 2.0f;

    void Start()
    {
        popupPanel?.SetActive(false);

        confirmButton?.onClick.AddListener(ConfirmExit);
        cancelButton?.onClick.AddListener(ClosePopup);
    }

    public void ShowExitPopup()
    {
        if ( popupPanel != null && playerCamera != null )
        {
            popupPanel.transform.position = playerCamera.position + playerCamera.forward * popupDistance;
            popupPanel.transform.LookAt(playerCamera);
            popupPanel.transform.Rotate(0, 180, 0);
            popupPanel.SetActive(true);
        }
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