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

public class Login : MonoBehaviour
{
    public TMP_InputField emailInput, passwordInput;
    public Button loginButton, signUpButton;
    public TextMeshProUGUI errorText;

    private FirebaseAuth auth;

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
            }
        });

        loginButton?.onClick.AddListener(OnLoginButtonClick);
        signUpButton?.onClick.AddListener(() => SceneManager.LoadScene("SignUpScene"));
    }

    public void OnLoginButtonClick()
    {
        string email = emailInput?.text.Trim();
        string password = passwordInput?.text.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Email and Password are required.");
            return;
        }

        if (auth == null)
        {
            ShowError("Invalid email or password. Please try again.");
            return;
        }

        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(loginTask =>
        {
            if (loginTask.IsFaulted || loginTask.IsCanceled)
            {
                ShowError("Invalid email or password. Please try again.");
                return;
            }
            if (auth.CurrentUser != null)
            {
                string userId = auth.CurrentUser.UserId;
                FirebaseDatabase.DefaultInstance.RootReference.Child("REVIRA").Child("Consumers").Child(userId)
                    .GetValueAsync().ContinueWithOnMainThread(dbTask =>
                    {
                        if (dbTask.IsFaulted || dbTask.IsCanceled || !dbTask.Result.Exists)
                        {
                            ShowError("Failed to load user data.");
                            return;
                        }

                        DataSnapshot snapshot = dbTask.Result;

                        string firstName = snapshot.Child("firstName").Value?.ToString() ?? "Not Added";
                        string lastName = snapshot.Child("lastName").Value?.ToString() ?? "Not Added";
                        string userEmail = snapshot.Child("email").Value?.ToString() ?? "Not Added";
                        float accountBalance = float.Parse(snapshot.Child("accountBalance").Value?.ToString() ?? "0");
                        string gender = snapshot.Child("gender").Exists ? snapshot.Child("gender").Value.ToString() : "Not Added";
                        string phone = snapshot.Child("phoneNumber").Exists ? snapshot.Child("phoneNumber").Value.ToString() : "Not Added";

                        UserManager.Instance.SetUserData(userId, firstName, lastName, userEmail, accountBalance, gender, phone);
                        SceneManager.LoadScene("HomeScene");
                    });
            }
        });
    }

    void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = Color.red;
        }
    }
}