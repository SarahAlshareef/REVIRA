// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// Firebase
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
// C#
using System.Collections;

public class RetrievePassword : MonoBehaviour
{
    public GameObject popupPanel;
    public TMP_InputField emailInput;
    public Button retrieveButton, closeButton;
    public TextMeshProUGUI feedbackText;

    private FirebaseAuth auth;

    void Start()
    {
        popupPanel.SetActive(false);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                retrieveButton.interactable = true;
            }
            else
            {
                ShowFeedback("Error initializing Firebase.", Color.red);
                retrieveButton.interactable = false;
            }  
        });

        retrieveButton.onClick.AddListener(() => StartCoroutine(RetrievePass(emailInput.text.Trim())));
        closeButton.onClick.AddListener(ClosePopup);
    }

    private IEnumerator RetrievePass(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            ShowFeedback("Please enter a valid email address.",Color.red);
            yield break;
        }

        retrieveButton.interactable = false;
        ShowFeedback("Processing request...", Color.yellow);


        var retrieveTask = auth.SendPasswordResetEmailAsync(email);

            yield return new WaitUntil(() => retrieveTask.IsCompleted);

            if (retrieveTask.IsCanceled)
            {
                ShowFeedback("Operation failed, please try again.", Color.red);
            }
            else if (retrieveTask.IsFaulted)
            {
                ShowFeedback("The email address is not registered, please try again.", Color.red);
            }
            else
            {
                ShowFeedback("A password reset link sent to your email successfully.", Color.green);
                yield return new WaitForSeconds(2f);
                ClosePopup();
            }           
        retrieveButton.interactable = true;
    }

    public void OpenPopup()
    {
        popupPanel.SetActive(true);
    }
    public void ClosePopup()
    {
        popupPanel.SetActive(false);
    }
    
    public void ShowFeedback(string message, Color color)
    {
        feedbackText.text = message;
        feedbackText.color = color;
    }
}
