// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
// Firebase
using Firebase;
using Firebase.Auth;
// C#
using System.Collections;
using System.Collections.Generic;
using Firebase.Database;

public class Login : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField emailInput, passwordInput;  
    public Button loginButton, signUpButton;   
    public TextMeshProUGUI errorText;     

    private FirebaseAuth auth;  

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
            auth = FirebaseAuth.DefaultInstance;
        }
    }

    public void OnLoginButtonClick()
    {
        // Ensure UI fields are updated before validation
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);

        string email = emailInput?.text.Trim();
        string password = passwordInput?.text;


        if (ValidateInputs(email, password))
        {
            StartCoroutine(LoginUser(email, password));
        }
    }

    private bool ValidateInputs(string email, string password)
    {
        List<string> missingFields = new List<string>();

        if (string.IsNullOrWhiteSpace(email))
            missingFields.Add("Email");

        if (string.IsNullOrWhiteSpace(password))
            missingFields.Add("Password");

        // If there are missing fields, display them all at once
        if (missingFields.Count > 0)
        {
            ShowError($"{string.Join(", ", missingFields)} {(missingFields.Count == 1 ? "is" : "are")} required.");
            return false; 
        }
        return true; 
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
        FirebaseUser user = auth.CurrentUser;

        if (user != null)
        {
            StartCoroutine(FetchUserData(user.UserId));
        }
    }

    IEnumerator FetchUserData(string userId)
    {
        DatabaseReference dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        var dbTask = dbReference.Child("users").Child(userId).GetValueAsync();
        yield return new WaitUntil(() => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            ShowError("Failed to load user data.");
            yield break;
        }

        if (dbTask.Result.Exists)
        {
            DataSnapshot snapshot = dbTask.Result;

            string firstName = snapshot.Child("firstName").Value.ToString();
            string lastName = snapshot.Child("lastName").Value.ToString();
            string email = snapshot.Child("email").Value.ToString();
            float accountBalance = float.Parse(snapshot.Child("accountBalance").Value.ToString());

            UserManager.Instance.SetUserData(userId, firstName, lastName, email, accountBalance);
            SceneManager.LoadScene("HomeScene");
        }
        else
        {
            ShowError("User data not found.");
        }
    }

    public void SignUp()
    {
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