// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
// Firebase
using Firebase.Auth;

public class Logout : MonoBehaviour
{
    public GameObject popupPanel; 
    public Button confirmButton, cancelButton;

    void Start()
    {
        popupPanel.SetActive(false);

        confirmButton?.onClick.AddListener(ConfirmLogout);
        cancelButton?.onClick.AddListener(ClosePopup);
    }

    public void ShowLogoutPopup()
    {
        popupPanel.SetActive(true);
    }

    private void ConfirmLogout()
    {
        UserManager.Instance.SetUserData("", "", "", "", 0);
        FirebaseAuth.DefaultInstance.SignOut();
   
        popupPanel.SetActive(false);
        SceneManager.LoadScene("MainMenu");
    }

    private void ClosePopup()
    {
        popupPanel.SetActive(false);
    }
}