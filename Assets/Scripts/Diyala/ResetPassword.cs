// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// Firebase
using Firebase.Auth;
using Firebase.Extensions;
// C#
using System.Collections;
using System.Collections.Generic;

public class ResetPassword : MonoBehaviour
{
    [Header("Game Objects")]
    public GameObject resetPanel;
    public GameObject Greeting;

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
        if (resetPanel.activeSelf)
        {
            CloseResetPanel();
        }
        else
        {
            resetPanel.SetActive(true);
            Greeting.SetActive(false);

            emailText.text = UserManager.Instance.Email;
            currentPasswordInput.text = "";
            newPasswordInput.text = "";
            confirmPasswordInput.text = "";
            messageText.text = "";
        }
    }

    void CloseResetPanel()
    {
        resetPanel.SetActive(false);
        Greeting.SetActive(true);
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

        var user = auth.CurrentUser;
        var credential = EmailAuthProvider.GetCredential(user.Email, currentPassword);


        user.ReauthenticateAsync(credential).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && !task.IsFaulted && !task.IsCanceled)
            {
                user.UpdatePasswordAsync(newPassword).ContinueWithOnMainThread(updateTask =>
                {
                    if (updateTask.IsCompleted && !updateTask.IsFaulted)
                    {
                        ShowMessage("Password updated successfully.", Color.green);
                    }
                    else
                    {
                        ShowMessage("Failed to update password. Try again.", Color.red);
                    }
                });
            }
            else
            {
                ShowMessage("Current password is incorrect.", Color.red);
            }
        });
    }

    void ShowMessage(string message, Color color)
    {
        messageText.text = message;
        messageText.color = color;
    }
}