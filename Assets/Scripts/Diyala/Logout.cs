using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;
using UnityEngine.SceneManagement;

public class Logout : MonoBehaviour
{
    public GameObject popupPanel; // Assign in the Inspector
    public Button confirmButton;
    public Button cancelButton;
    public TextMeshProUGUI popupMessage;

    void Start()
    {
        // Hide popup initially
        popupPanel.SetActive(false);

        // Add button listeners
        confirmButton.onClick.AddListener(ConfirmLogout);
        cancelButton.onClick.AddListener(ClosePopup);
    }

    // Function to show the popup
    public void ShowLogoutPopup()
    {
        popupPanel.SetActive(true);
    }

    private void ConfirmLogout()
    {
        FirebaseAuth.DefaultInstance.SignOut();
        Debug.Log("User logged out successfully.");

        // Close popup and load MainMenu scene
        popupPanel.SetActive(false);
        SceneManager.LoadScene("MainMenu");
    }

    private void ClosePopup()
    {
        popupPanel.SetActive(false);
    }
}