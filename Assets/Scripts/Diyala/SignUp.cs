using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using TMPro;

public class SignUp : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField firstNameInput;   // Input field for first name
    public TMP_InputField lastNameInput;    // Input field for last name
    public TMP_InputField emailInput;       // Input field for email
    public TMP_InputField passwordInput;    // Input field for password
    public Button signUpButton;             // Sign-up button
    public Button loginButton;              // Login button
    public TextMeshProUGUI errorText;       // UI element to display error messages

    private FirebaseAuth auth;  // Firebase Authentication instance

    void Start()
    {
        // Initialize Firebase Authentication
        StartCoroutine(InitializeFirebase());

        // Assign button click events
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
            // Log and show error if Firebase fails to initialize
            Debug.LogError("Firebase initialization failed: " + task.Exception);
            ShowError("Firebase setup failed.");
        }
        else
        {
            // Initialize Firebase Authentication
            auth = FirebaseAuth.DefaultInstance;
        }
    }

    public void OnSignUpButtonClick()
    {
        // Get trimmed input values
        string firstName = firstNameInput?.text.Trim();
        string lastName = lastNameInput?.text.Trim();
        string email = emailInput?.text.Trim();
        string password = passwordInput?.text;

        // Debugging: Log input values
        Debug.Log($"First Name: [{firstName}], Last Name: [{lastName}], Email: [{email}], Password: [{password}]");

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
        if (string.IsNullOrEmpty(firstName))
            missingFields.Add("First Name");

        if (string.IsNullOrEmpty(lastName))
            missingFields.Add("Last Name");

        if (string.IsNullOrEmpty(email))
            missingFields.Add("Email");

        if (string.IsNullOrEmpty(password))
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
            // If sign-up is successful, navigate to the login scene
            Debug.Log("Sign Up Successful!");
            SceneManager.LoadScene("LoginScene");
        }
    }

    public void GoToLoginScene()
    {
        // Load the login scene when the user clicks the login button
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