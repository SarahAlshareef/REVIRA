using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Login : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField emailInput;       // Input field for email
    public TMP_InputField passwordInput;    // Input field for password
    public Button loginButton;              // Login button
    public Button signUpButton;             // Button to switch to Sign-Up scene
    public TextMeshProUGUI errorText;       // UI text for error messages

    private FirebaseAuth auth;  // Firebase Authentication instance
    private FirebaseUser user;  // Stores the logged in user's data

    void Start()
    {
        // Initialize Firebase Authentication
        StartCoroutine(InitializeFirebase());

        // Assign button click events
        if (loginButton != null)
            loginButton.onClick.AddListener(OnLoginButtonClick);

        if (signUpButton != null)
            signUpButton.onClick.AddListener(SignUp);
    }

    IEnumerator InitializeFirebase()
    {
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
        }
    }

    public void OnLoginButtonClick()
    {
        // Ensure UI fields are updated before validation
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        string email = emailInput?.text.Trim();
        string password = passwordInput?.text;

        // Debugging: Log input values
        Debug.Log($"Attempting login with Email: [{email}]");

        // Validate inputs before attempting login
        if (ValidateInputs(email, password))
        {
            StartCoroutine(LoginUser(email, password));
        }
    }

    private bool ValidateInputs(string email, string password)
    {
        List<string> missingFields = new List<string>();

        // Check for empty fields and add them to the list
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

    IEnumerator LoginUser(string email, string password)
    {
        // Ensure Firebase is initialized before attempting login
        if (auth == null)
        {
            ShowError("Authentication service not available. Try again later.");
            Debug.LogError("Firebase Authentication is not initialized.");
            yield break;
        }

        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.IsCanceled || loginTask.IsFaulted)
        {
            Debug.LogError("Error logging in: " + loginTask.Exception);

            // Extract Firebase-specific error code safely
            FirebaseException firebaseEx = loginTask.Exception?.GetBaseException() as FirebaseException;
            AuthError errorCode = AuthError.None; // Default to None

            if (firebaseEx != null && firebaseEx.ErrorCode != 0)
            {
                errorCode = (AuthError)firebaseEx.ErrorCode;
            }

            // Default error message
            string errorMessage = "An unknown error occurred. Please try again.";

            // Handle Firebase authentication errors
            switch (errorCode)
            {
                case AuthError.InvalidEmail:
                    errorMessage = "Invalid email format. Please enter a valid email.";
                    break;
                case AuthError.WrongPassword:
                    errorMessage = "Incorrect password. Please try again.";
                    break;
                case AuthError.UserNotFound:
                    errorMessage = "This email is not registered. Please sign up.";
                    break;
                case AuthError.UserDisabled:
                    errorMessage = "This account has been disabled.";
                    break;
                case AuthError.NetworkRequestFailed:
                    errorMessage = "Network error. Check your internet connection.";
                    break;
                default:
                    // If Firebase didn't classify the error, check the raw error message
                    string rawErrorMessage = firebaseEx != null ? firebaseEx.Message : "";

                    if (!string.IsNullOrEmpty(rawErrorMessage))
                    {
                        if (rawErrorMessage.Contains("EMAIL_NOT_FOUND"))
                        {
                            errorMessage = "This email is not registered. Please sign up.";
                        }
                        else if (rawErrorMessage.Contains("INVALID_EMAIL"))
                        {
                            errorMessage = "Invalid email format. Please enter a valid email.";
                        }
                    }
                    break;
            }

            // Display the error message to the user
            ShowError(errorMessage);
            yield break;
        }

        // Successful login
        user = loginTask.Result?.User;
        if (user != null)
        {
            Debug.Log("User logged in successfully: " + user.Email);
            SceneManager.LoadScene("HomeScene");
        }
        else
        {
            ShowError("An error occurred while retrieving user data. Please try again.");
            Debug.LogError("Firebase login succeeded but user data is null.");
        }
    }

    public void SignUp()
    {
        // Load the sign-up scene
        SceneManager.LoadScene("SignUpScene");
    }

    void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = Color.red;
        }
        Debug.LogError("Displayed Error: " + message);
    }
}