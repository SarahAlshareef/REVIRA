// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// Firebase
using Firebase;
using Firebase.Auth;
// C#
using System.Collections;
using System.Collections.Generic;

public class PasswordReset : MonoBehaviour
{
    public TMP_InputField email;
    public Button reset;
    public TextMeshProUGUI feedbackText;

    private FirebaseAuth auth;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
            }
        });

        reset.onClick.AddListener(() => ResetPassword(email.text));
    }
    public void ResetPassword(string Email)
    {
        if (string.IsNullOrEmpty(Email))
        {
            feedbackText.text = "Please enter a valid email address.";
            feedbackText.color = Color.red;
            return;
        }
        auth.SendPasswordResetEmailAsync(email).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                feedbackText.text = "Operation failed, please try again.";
                feedbackText.color = Color.red;
                return;
            }
            if (task.IsFaulted)
            {
                feedbackText.text = "The email address is not registered, please try again.";
                feedbackText.color = Color.red;
                return;
            }

            feedbackText.text = "A password reset link has been sent to your email successfully.";
            feedbackText.color = Color.green;
        });
    }
}
