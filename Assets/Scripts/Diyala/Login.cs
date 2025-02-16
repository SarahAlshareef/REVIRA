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
    private FirebaseAuth auth;
    private FirebaseUser user;

    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public Button signUpButton;
    public TextMeshProUGUI errorText;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == DependencyStatus.Available)
            {
                FirebaseApp app = FirebaseApp.DefaultInstance;
                auth = FirebaseAuth.DefaultInstance;
                Debug.Log("Firebase initialized successfully.");
            }
            else
            {
                Debug.LogError("Could not resolve Firebase dependencies: " + task.Result);
                ShowError("Firebase setup error.");
            }
        });

        loginButton.onClick.AddListener(LoginUser);
    }

    public void SignUp()
    {
        SceneManager.LoadScene("SignUpScene");
    }

    public void LoginUser()
    {
        if (auth == null)
        {
            ShowError("Authentication service not available. Try again later.");
            Debug.LogError("Firebase Authentication is not initialized.");
            return;
        }

        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            ShowError("Please enter both email and password.");
            return;
        }

        Debug.Log($"Attempting login with Email: {email}");

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {

            if (task.IsCanceled)
            {
                ShowError("Login was canceled.");
                Debug.LogError("Login task was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("Error logging in: " + task.Exception);
                FirebaseException firebaseEx = task.Exception.GetBaseException() as FirebaseException;
                AuthError errorCode = (AuthError)firebaseEx.ErrorCode;

                string errorMessage = "Login failed. Check your email and password.";
                switch (errorCode)
                {
                    case AuthError.InvalidEmail:
                        errorMessage = "Invalid email format.";
                        break;
                    case AuthError.WrongPassword:
                        errorMessage = "Incorrect password.";
                        break;
                    case AuthError.UserNotFound:
                        errorMessage = "No user found with this email.";
                        break;
                    case AuthError.UserDisabled:
                        errorMessage = "This account has been disabled.";
                        break;
                    case AuthError.NetworkRequestFailed:
                        errorMessage = "Network error. Check your connection.";
                        break;
                    default:
                        errorMessage = firebaseEx.Message;
                        break;
                }

                ShowError(errorMessage);
                return;
            }

            user = task.Result.User;
            Debug.Log("User logged in successfully: " + user.Email);
            SceneManager.LoadScene("HomeScene");
        });
    }

    void ShowError(string message)
    {
        errorText.text = message;
        errorText.color = Color.red;
        Debug.LogError("Displayed Error: " + message);
    }
}