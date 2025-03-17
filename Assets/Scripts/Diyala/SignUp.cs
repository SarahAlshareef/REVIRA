// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
// Firebase
using Firebase;
using Firebase.Auth;
using Firebase.Database;
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
        // Initialize Firebase Authentication
        StartCoroutine(InitializeFirebase());

        signUpButton?.onClick.AddListener(OnSignUpButtonClick);
        loginButton?.onClick.AddListener(GoToLoginScene);
    }

    IEnumerator InitializeFirebase()
    {
        // Check and fix Firebase dependencies
        var task = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)
        {
            Debug.LogError("Firebase initialization failed: " + task.Exception);
            ShowError("Firebase setup failed.");
        }
        else
        {
            auth = FirebaseAuth.DefaultInstance;
            dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        }
    }

    public void OnSignUpButtonClick()
    {
        // Get trimmed input values
        string firstName = firstNameInput?.text.Trim();
        string lastName = lastNameInput?.text.Trim();
        string email = emailInput?.text.Trim();
        string password = passwordInput?.text;

        // Validate inputs before attempting sign-up
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

        // Attempt to create a new user with email and password
        var signUpTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => signUpTask.IsCompleted);

        // Check if the sign-up operation failed
        if (signUpTask.IsCanceled || signUpTask.IsFaulted)
        {
            HandleSignUpError(signUpTask.Exception);
        }
        else
        {
            // If sign-up is successful, get the user
            FirebaseUser newUser = auth.CurrentUser;

            if (newUser != null)
            {
                // Create a profile update request
                UserProfile profile = new UserProfile
                {
                    DisplayName = $"{firstName} {lastName}"
                };

                // Apply the profile update
                var profileUpdateTask = newUser.UpdateUserProfileAsync(profile);
                yield return new WaitUntil(() => profileUpdateTask.IsCompleted);

                StartCoroutine(SaveUserToDatabase(newUser.UserId, firstName, lastName, email));
            }
        }
    }

    void HandleSignUpError(AggregateException exception)
    {

        // Extract Firebase-specific error code
        if (exception.GetBaseException() is FirebaseException firebaseEx)
        {
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;
            string errorMessage = errorCode switch
            {
                AuthError.EmailAlreadyInUse => "This email is already in use.",
                AuthError.InvalidEmail => "Invalid email format.",
                AuthError.WeakPassword => "Password is too weak.",
                AuthError.NetworkRequestFailed => "Network error. Check your connection.",
                _ => firebaseEx.Message
            };
            ShowError(errorMessage);
        }
        else
        {
            ShowError("An unkown error occurerd.");
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
            { "accountBalance", 1000 }

        };

        var dbTask = dbReference.Child("users").Child(userId).SetValueAsync(userData);
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