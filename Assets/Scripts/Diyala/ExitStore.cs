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