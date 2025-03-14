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
using System.Text.RegularExpressions;
using System.Data.Common;

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

        if (signUpButton != null)
            signUpButton.onClick.AddListener(OnSignUpButtonClick);

        if (loginButton != null)
            loginButton.onClick.AddListener(GoToLoginScene);
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

    private bool ValidateInputs(string firstName, string lastName, string email, string password)
    {
        // List to store missing fields
        List<string> missingFields = new List<string>();

        // Check for empty fields and add them to the list
        if (string.IsNullOrWhiteSpace(firstName))
            missingFields.Add("First Name");

        if (string.IsNullOrWhiteSpace(lastName))
            missingFields.Add("Last Name");

        if (string.IsNullOrWhiteSpace(email))
            missingFields.Add("Email");

        if (string.IsNullOrWhiteSpace(password))
            missingFields.Add("Password");

        // If there are missing fields, display them all at once
        if (missingFields.Count > 0)
        {
            ShowError($"{string.Join(", ", missingFields)} {(missingFields.Count == 1 ? "is" : "are")} required.");
            return false; // Validation failed
        }
        return true; // Validation successful
    }

    IEnumerator SignUpUser(string firstName, string lastName, string email, string password)
    {
        // Attempt to create a new user with email and password
        var signUpTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => signUpTask.IsCompleted);

        // Check if the sign-up operation failed
        if (signUpTask.IsCanceled || signUpTask.IsFaulted)
        {
            Debug.LogError("Sign Up Failed: " + signUpTask.Exception);

            // Extract Firebase-specific error code
            FirebaseException firebaseEx = signUpTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            // Default error message
            string errorMessage = "Sign up failed. Please try again.";

            // Provide user-friendly error messages based on error code
            switch (errorCode)
            {
                case AuthError.EmailAlreadyInUse:
                    errorMessage = "This email is already in use.";
                    break;
                case AuthError.InvalidEmail:
                    errorMessage = "Invalid email format.";
                    break;
                case AuthError.WeakPassword:
                    errorMessage = "Password is too weak.";
                    break;
                case AuthError.NetworkRequestFailed:
                    errorMessage = "Network error. Check your connection.";
                    break;
                default:
                    errorMessage = firebaseEx.Message;
                    break;
            }
            // Display the error message to the user
            ShowError(errorMessage);
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

                if (profileUpdateTask.Exception != null)
                {
                    Debug.LogError("Failed to update user profile: " + profileUpdateTask.Exception);
                }
                else
                {
                    Debug.Log("User profile updated successfully!");
                }
                    StartCoroutine(SaveUserToDatabase(newUser.UserId, firstName, lastName, email));
            }
        }
    }

    IEnumerator SaveUserToDatabase(string userId, string firstName, string lastName, string email)
    {
 
            Dictionary<string, object> userData = new Dictionary<string, object>
        {
            { "userId", userId },
            { "firstName", firstName },
            { "lastName", lastName },
            { "email", email }
        };

        var dbTask = dbReference.Child("users").Child(userId).SetValueAsync(userData);
        yield return new WaitUntil(() => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.LogError("Failed to save user data: " + dbTask.Exception);
        }
        else
        {
            GoToLoginScene();
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
            errorText.text = message; // Update error text
            errorText.color = Color.red;
        }
        // Debugging: Log error message
        Debug.LogError("Displayed Error: " + message);
    }
}