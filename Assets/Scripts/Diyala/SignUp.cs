// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
// Firebase
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.AppCheck;
// C#
using System;
using System.Collections;
using System.Collections.Generic;

public class SignUp : MonoBehaviour
{
    
    public TMP_InputField firstNameInput, lastNameInput, emailInput, passwordInput;   
    public Button signUpButton, loginButton; 
    public TextMeshProUGUI errorText;

    private FirebaseAuth auth;
    private DatabaseReference dbReference;

    private bool firebaseReady = false;

    void Start()
    {
        FirebaseAppCheck.SetAppCheckProviderFactory(null);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null)
            {
                ShowError("Firebase Dependency check failed.");
                return;
            }
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
                firebaseReady = true;
            }
            else
            {
                ShowError("Firebase failed to initialize.");
            }
        });

        signUpButton?.onClick.AddListener(OnSignUpButtonClick);
        loginButton?.onClick.AddListener(GoToLoginScene);
    }

    public void OnSignUpButtonClick()
    {
        if (!firebaseReady)
        {
            ShowError("Firebase is not initialized yet.");
            return;
        }

        string firstName = firstNameInput?.text.Trim();
        string lastName = lastNameInput?.text.Trim();
        string email = emailInput?.text.Trim();
        string password = passwordInput?.text.Trim();

        List<string> missingFields = new();
        if (string.IsNullOrWhiteSpace(firstName)) missingFields.Add("First Name");
        if (string.IsNullOrWhiteSpace(lastName)) missingFields.Add("Last Name");
        if (string.IsNullOrWhiteSpace(email)) missingFields.Add("Email");
        if (string.IsNullOrWhiteSpace(password)) missingFields.Add("Password");

        if (missingFields.Count > 0)
        {
            ShowError($"{string.Join(", ", missingFields)} {(missingFields.Count == 1 ? "is" : "are")} required.");
            return;
        }

        if ( auth == null)
        {
            ShowError("Authentication service is not initiated.");
            return;
        }

        var signUpTask = auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(signUpTask =>
        {
            if (signUpTask.IsCanceled || signUpTask.IsFaulted)
            {
                HandleSignUpError(signUpTask.Exception);
                return;
            }

            if (auth.CurrentUser == null)
            {
                ShowError("User creation failed.");
                return;
            }

            string userId = auth.CurrentUser.UserId;
            Dictionary<string, object> userData = new()
            {
                { "userId", userId },
                { "firstName", firstName },
                { "lastName", lastName },
                { "email", email },
                { "accountBalance", 0 }
            };

            var dbTask = dbReference.Child("REVIRA").Child("Consumers").Child(userId).SetValueAsync(userData);
            dbTask.ContinueWithOnMainThread(saveTask =>
            {
                if (saveTask.IsFaulted || saveTask.IsCanceled)
                {
                    ShowError("Failed to save user data: " + saveTask.Exception?.Message);
                    return;
                }
                GoToLoginScene();
            });
        });                
    }

    void HandleSignUpError(AggregateException exception)
    {

        if (exception.GetBaseException() is FirebaseException firebaseEx)
        {
            string errorMessage = firebaseEx.ErrorCode switch
            {
                (int)AuthError.EmailAlreadyInUse => "This email is already in use.",
                (int)AuthError.InvalidEmail => "Invalid email format.",
                (int)AuthError.WeakPassword => "Password is too weak.",
                (int)AuthError.NetworkRequestFailed => "Network error. Check your connection.",
                _ => firebaseEx.Message
            };
            ShowError(errorMessage);
        }
        else
        {
            ShowError("An unknown error occurred.");
        }
    }

    public void GoToLoginScene()
    {
        SceneManager.LoadScene("LoginScene");
    }

    void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message; 
            errorText.color = Color.red;
        }
    }
}