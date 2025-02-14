using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

public class LoginController : MonoBehaviour
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
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                Debug.Log("Firebase Auth initialized successfully.");
            }
            else
            {
                Debug.LogError("Could not resolve Firebase dependencies: " + task.Result);
                ShowError("Firebase setup error. Please check your configuration.");
            }
        });

        loginButton.onClick.AddListener(LoginUser);
        signUpButton.onClick.AddListener(SignUp);
    }

    public void SignUp()
    {
        SceneManager.LoadScene("SignUpScene");
    }


public void LoginUser()
{
    string email = emailInput.text.Trim();
    string password = passwordInput.text.Trim();

    // Check if email is in valid format
    if (!IsValidEmail(email))
    {
        ShowError("Please enter a valid email address.");
        return;
    }

    if (string.IsNullOrEmpty(password))
    {
        ShowError("Please enter your password.");
        return;
    }

    auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
    {
        if (task.IsCanceled || task.IsFaulted)
        {
            ShowError("Login failed. Check your email and password.");
            Debug.LogError("Login Error: " + task.Exception);
            return;
        }

        user = task.Result.User;
        Debug.Log("User logged in successfully: " + user.Email);
        SceneManager.LoadScene("HomeScene");
    });
}

// Function to validate email format
bool IsValidEmail(string email)
{
    string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$"; // Regex for valid email
    return Regex.IsMatch(email, emailPattern);
}


void ShowError(string message)
    {
        errorText.text = message;
        errorText.color = Color.red;
    }
}
