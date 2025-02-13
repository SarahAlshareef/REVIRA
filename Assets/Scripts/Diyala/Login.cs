using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Login : MonoBehaviour {

    private FirebaseAuth auth;
    private FirebaseUser user;

    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public Button signUpButton;
    public TextMeshProUGUI errorText;
    

    void Start() {
        // Initialize Firebase Auth
        auth = FirebaseAuth.DefaultInstance; 

        // Assign button listeners
        loginButton.onClick.AddListener(LoginUser);
        signUpButton.onClick.AddListener(() => SceneManager.LoadScene("SignUpScene"));
    }

    public void LoginUser()
    {
        string email = emailInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password)) 
        {
            ShowError("Please enter both email and password.");
            return;
        }

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
          
            if (task.IsCanceled) {
                ShowError("Login was canceled.");
                return;
            }
            if (task.IsFaulted) {
                ShowError("Login failed. Check your email and password.");
                return;
            }

            user = task.Result.User;
            Debug.Log("User logged in successfully: " + user.Email);
            SceneManager.LoadScene("HomeScene");
        });
    }
    void ShowError(string message) {
        errorText.text = message;
        errorText.color = Color.red;
    }
}
