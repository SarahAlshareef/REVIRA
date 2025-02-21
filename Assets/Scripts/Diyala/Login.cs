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
        if (auth == null)
        {
            ShowError("Invalid email or password. Please try again.");
            yield break;
        }

        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.IsFaulted || loginTask.IsCanceled)
        {
            ShowError("Invalid email or password. Please try again.");
            yield break;
        }

        // Successful login
        SceneManager.LoadScene("HomeScene");
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