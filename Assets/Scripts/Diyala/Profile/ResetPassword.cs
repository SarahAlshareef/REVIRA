// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// Firebase
using Firebase.Auth;
using Firebase.Extensions;
// C#
using System.Collections;


public class ResetPassword : MonoBehaviour
{
    [Header("Game Objects")]
    public GameObject resetPanel;

    [Header("Buttons")]
    public Button resetPasswordButton;
    public Button cancelButton;

    [Header("Input Fields")]
    public TMP_InputField currentPasswordInput;
    public TMP_InputField newPasswordInput;
    public TMP_InputField confirmPasswordInput;

    [Header("Text Elements")]
    public TextMeshProUGUI emailText;
    public TextMeshProUGUI messageText;

    private Coroutine messageCoroutine;
    private FirebaseAuth auth;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;

        resetPanel.SetActive(false);

        resetPasswordButton?.onClick.AddListener(HandlePasswordReset);
        cancelButton?.onClick.AddListener(CloseResetPanel);
    }

    public void OpenResetPanel()
    {
            resetPanel.SetActive(true);
            emailText.text = UserManager.Instance.Email;
    }

    public void CloseResetPanel()
    {
        resetPanel.SetActive(false);
        currentPasswordInput.text = "";
        newPasswordInput.text = "";
        confirmPasswordInput.text = "";
        messageText.text = "";
    }

    void HandlePasswordReset()
    {
        string currentPassword = currentPasswordInput.text.Trim();
        string newPassword = newPasswordInput.text.Trim();
        string confirmPassword = confirmPasswordInput.text.Trim();

        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
        {
            ShowMessage("Please fill in all fields.", Color.red);
            return;
        }

        if (newPassword != confirmPassword)
        {
            ShowMessage("New passwords do not match.", Color.red);

            newPasswordInput.text = "";
            confirmPasswordInput.text = "";

            return;
        }

        if (newPassword.Length < 8)
        {
            ShowMessage("Password must be at least 8 characters.", Color.red);

            newPasswordInput.text = "";
            confirmPasswordInput.text = "";

            return;
        }

        if (currentPassword == newPassword)
        {
            ShowMessage("New password cannot be the same as current password.", Color.red);

            newPasswordInput.text = "";
            confirmPasswordInput.text = "";

            return;
        }

        resetPasswordButton.interactable = false;
        ShowMessage("Processing request...", Color.yellow);

        var user = auth.CurrentUser;
        var credential = EmailAuthProvider.GetCredential(user.Email, currentPassword);


        user.ReauthenticateAsync(credential).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompletedSuccessfully)
            {
                user.UpdatePasswordAsync(newPassword).ContinueWithOnMainThread(updateTask =>
                {
                    if (updateTask.IsCompletedSuccessfully)
                    {
                        ShowMessage("Password updated successfully.", Color.green);
                    }
                    else
                    {
                        ShowMessage("Failed to update password. Try again.", Color.red);
                    }
                    resetPasswordButton.interactable = true;
                });
            }
            else
            {
                ShowMessage("Current password is incorrect.", Color.red);
                resetPasswordButton.interactable = true;
            }
        });      
    }

    void ShowMessage(string message, Color color)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = color;
            messageText.gameObject.SetActive(true);

            if (messageCoroutine != null) 
                StopCoroutine(messageCoroutine);

            messageCoroutine = StartCoroutine(HideMessageAfterDelay(3f));
        }      
    }

    IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        messageText.gameObject.SetActive(false);
    }
}