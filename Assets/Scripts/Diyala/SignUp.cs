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
// C#
using System.Collections;
using System.Collections.Generic;
using System;

public class SignUp : MonoBehaviour
{
    
    public TMP_InputField firstNameInput, lastNameInput, emailInput, passwordInput;   
    public Button signUpButton, loginButton; 
    public TextMeshProUGUI errorText;

    private FirebaseAuth auth;
    private DatabaseReference dbReference;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                dbReference = FirebaseDatabase.DefaultInstance.RootReference;
            }
        });

        signUpButton?.onClick.AddListener(OnSignUpButtonClick);
        loginButton?.onClick.AddListener(GoToLoginScene);
    }
    public void OnSignUpButtonClick()
    {
        string firstName = firstNameInput?.text.Trim();
        string lastName = lastNameInput?.text.Trim();
        string email = emailInput?.text.Trim();
        string password = passwordInput?.text;

        if (ValidateInputs(firstName, lastName, email, password))
        {
            StartCoroutine(SignUpUser(firstName, lastName, email, password));
        }
    }
    bool ValidateInputs(string firstName, string lastName, string email, string password)
    {
        List<string> missingFields = new();
        if (string.IsNullOrWhiteSpace(firstName)) missingFields.Add("First Name");
        if (string.IsNullOrWhiteSpace(lastName)) missingFields.Add("Last Name");
        if (string.IsNullOrWhiteSpace(email)) missingFields.Add("Email");
        if (string.IsNullOrWhiteSpace(password)) missingFields.Add("Password");

        if (missingFields.Count > 0)
        {
            ShowError($"{string.Join(", ", missingFields)} {(missingFields.Count == 1 ? "is" : "are")} required.");
            return false;
        }
        return true;
    }
    IEnumerator SignUpUser(string firstName, string lastName, string email, string password)
    {
        if ( auth == null)
        {
            ShowError("Authentication service is not initiated.");
            yield break;
        }

        var signUpTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => signUpTask.IsCompleted);

        if (signUpTask.IsCanceled || signUpTask.IsFaulted)
        {
            HandleSignUpError(signUpTask.Exception);
            yield break;
        }
 
        if (auth.CurrentUser == null) yield break;
        StartCoroutine(SaveUserToDatabase(auth.CurrentUser.UserId, firstName, lastName, email));             
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
    IEnumerator SaveUserToDatabase(string userId, string firstName, string lastName, string email)
    {
        Dictionary<string, object> userData = new Dictionary<string, object>
        {
            { "userId", userId },
            { "firstName", firstName },
            { "lastName", lastName },
            { "email", email },
            { "accountBalance", 0 }
        };

        var dbTask = dbReference.Child("REVIRA").Child("Consumers").Child(userId).SetValueAsync(userData);
        yield return new WaitUntil(() => dbTask.IsCompleted);

        if (dbTask.Exception != null)
            Debug.LogError("Failed to save user data: " + dbTask.Exception);
        else
            GoToLoginScene();
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