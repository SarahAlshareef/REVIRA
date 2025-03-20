// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// Firebase
using Firebase;
using Firebase.Auth;
using Firebase.Extentions;
// C#
using System.Collections;
using System.Collections.Generic;

public class RetrievePassword : MonoBehaviour
{
    public TMP_InputField emailInput;
    public Button retrieve;
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
            else
            {
                feedbackText.text = "Error intializing Firebase.";
                feedbackText.color = Color.red;
            }  
        });
        retrieve.onClick.AddListener(() => RetrievePass(emailInput.text));
    }

    public void RetrievePass(string email)
    {
        if (string.IsNullOrEmpty(email))
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
