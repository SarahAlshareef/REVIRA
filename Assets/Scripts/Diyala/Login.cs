// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
// Firebase
using Firebase;
using Firebase.Auth;
using Firebase.Database;
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
        StartCoroutine(InitializeFirebase());

        loginButton?.onClick.AddListener(OnLoginButtonClick);
        signUpButton?.onClick.AddListener(() => SceneManager.LoadScene("SignUpScene"));
    }

    IEnumerator InitializeFirebase()
    {
        var task = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.Exception != null)       
            ShowError("Firebase setup failed.");
        
        else    
            auth = FirebaseAuth.DefaultInstance; 
    }

    public void OnLoginButtonClick()
    {

        string email = emailInput?.text.Trim();
        string password = passwordInput?.text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Email and Password are required.");
        }
        else
        {
            StartCoroutine(LoginUser(email, password));
        }
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
        if (auth.CurrentUser != null)
            StartCoroutine(FetchUserData(auth.CurrentUser.UserId));
    }

    IEnumerator FetchUserData(string userId)
    {
        var dbTask = FirebaseDatabase.DefaultInstance.RootReference.Child("users").Child(userId).GetValueAsync();
        yield return new WaitUntil(() => dbTask.IsCompleted);

        if (dbTask.Exception != null || !dbTask.Result.Exists)
        {
            ShowError("Failed to load user data.");
            yield break;
        }

            DataSnapshot snapshot = dbTask.Result;

            string firstName = snapshot.Child("firstName").Value.ToString();
            string lastName = snapshot.Child("lastName").Value.ToString();
            string email = snapshot.Child("email").Value.ToString();
            float accountBalance = float.Parse(snapshot.Child("accountBalance").Value.ToString());

            UserManager.Instance.SetUserData(userId, firstName, lastName, email, accountBalance);
            SceneManager.LoadScene("HomeScene");
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