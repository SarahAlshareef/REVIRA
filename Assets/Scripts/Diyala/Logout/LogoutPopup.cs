using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Firebase.Auth;

public class LogoutPopup : MonoBehaviour
{
    public Button confirmButton;  // Logout confirmation button
    public Button cancelButton;   // Cancel button
    public TextMeshProUGUI messageText; // Message text for confirmation

    void Start()
    {
        // Assign button listeners
        confirmButton.onClick.AddListener(ConfirmLogout);
        cancelButton.onClick.AddListener(ClosePopup);
    }

    public void ConfirmLogout()
    {
        // Sign out the user from Firebase
        FirebaseAuth.DefaultInstance.SignOut();
        Debug.Log("User logged out successfully.");

        // Remove the popup manager instance to reset it
        if (LogoutGlobalPopup.Instance != null)
        {
            LogoutGlobalPopup.Instance.DestroyPopup();
        }

        // Load the main menu scene
        SceneManager.LoadScene("MainMenu");
    }

    public void ClosePopup()
    {
        // Close the popup without logging out
        Destroy(gameObject);
    }
}