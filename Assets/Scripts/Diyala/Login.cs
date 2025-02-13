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


    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
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
