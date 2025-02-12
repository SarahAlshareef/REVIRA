using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine.UI;
using TMPro;

public class Login : MonoBehaviour {
    [Header("Firebase")]
    private FirebaseAuth auth;
    private FirebaseUser user;

    [Header("UI Elements")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public Button signUpButton;
    public TextMeshProUGUI messageText;

    void Start() {
        // Initialize Firebase
        auth = FirebaseAuth.DefaultInstance;

        // Add Listeners to buttons
        loginButton.onClick.AddListener(() => LoginUser(emailInput.text, passwordInput.text));
        signUpButton.onClick.AddListener(() => RegisterUser(emailInput.text, passwordInput.text));
    }

    void LoginUser(string email, string password) {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) {
            messageText.text = "Please enter both email and password.";
            return;
        }

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted) {
                messageText.text = "Login Failed: " + task.Exception.InnerExceptions[0].Message;
                return;
            }

            user = task.Result.User;
            messageText.text = "Login Successful! Welcome, " + user.Email;
        });
    }

    void RegisterUser(string email, string password) {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            messageText.text = "Please enter both email and password.";
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                messageText.text = "Sign Up Failed: " + task.Exception.InnerExceptions[0].Message;
                return;
            }

            user = task.Result.User;
            messageText.text = "Sign Up Successful! Welcome, " + user.Email;
        });
    }
}
