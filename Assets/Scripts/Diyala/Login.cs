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
    private Coroutine messageCoroutine;

    private FirebaseAuth auth;
    private DatabaseReference dbReference; 

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                dbReference = FirebaseDatabase.DefaultInstance.RootReference; 
            }
            else
            {
                ShowError("Firebase failed to initialize.");
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
            ShowError("Authentication service is not initiated.");
            return;
        }

        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(loginTask =>
        {

            if (loginTask.IsFaulted || loginTask.IsCanceled)
            {
                ShowError("Invalid email or password. Please try again.");
                return;
            }

            if (loginTask.Result.User != null)
            {
                string userId = loginTask.Result.User.UserId;
                LoadUserDataAndGoHome(userId);
            }
            else
            {
                ShowError("Login failed unexpectedly.");
            }
        });
    }

    private void LoadUserDataAndGoHome(string userId) 
    {

        if (dbReference == null)
        {
            ShowError("Database not ready. Try again.");
            return;
        }

        dbReference.Child("REVIRA").Child("Consumers").Child(userId).GetValueAsync().ContinueWithOnMainThread(dbTask =>
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


                if (UserManager.Instance != null)
                {
                    UserManager.Instance.SetUserData(userId, firstName, lastName, userEmail, accountBalance, gender, phone);
                    SceneManager.LoadScene("Store"); 
                }
                else
                {
                    ShowError("User manager not available.");
                }
            });
    }

    void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = Color.red;
            errorText.gameObject.SetActive(true);

            if (messageCoroutine != null)
                StopCoroutine(messageCoroutine);

            messageCoroutine = StartCoroutine(HideMessageAfterDelay(3f));
        }
    }
    IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        errorText.gameObject.SetActive(false);
    }
}
