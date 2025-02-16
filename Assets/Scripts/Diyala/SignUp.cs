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
    public TMP_InputField firstNameInput;
    public TMP_InputField lastNameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Button signUpButton;
    public Button loginButton;
    public TextMeshProUGUI errorText; // Added for error messages

    private FirebaseAuth auth;

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

    private bool ValidateInputs(string firstName, string lastName, string email, string password)
    {
        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
        {
            ShowError("First Name and Last Name are required.");
            return false;
        }
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowError("Email and Password are required.");
            return false;
        }
        return true;
    }

    IEnumerator SignUpUser(string firstName, string lastName, string email, string password)
    {
        var signUpTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => signUpTask.IsCompleted);

        if (signUpTask.IsCanceled || signUpTask.IsFaulted)
        {
            Debug.LogError("Sign Up Failed: " + signUpTask.Exception);

            FirebaseException firebaseEx = signUpTask.Exception.GetBaseException() as FirebaseException;
            AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

            string errorMessage = "Sign up failed. Please try again.";
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

            ShowError(errorMessage);
        }
        else
        {
            Debug.Log("Sign Up Successful!");
            SceneManager.LoadScene("LoginScene"); // Navigate to LooginScene
        }
    }

    public void GoToLoginScene()
    {
        SceneManager.LoadScene("LoginScene"); // Navigate to LoginScene
    }

    void ShowError(string message)
    {
        errorText.text = message;
        errorText.color = Color.red;
        Debug.LogError("Displayed Error: " + message);
    }
}