using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
public class SignUp2 : MonoBehaviour, IPointerClickHandler
{
    public TMP_InputField firstNameInput, lastNameInput, emailInput, passwordInput;
    public Button signUpButton, loginButton;
    public TextMeshProUGUI errorText;

    private FirebaseAuth auth;
    private DatabaseReference dbReference;

    void Start()
    {
        StartCoroutine(InitializeFirebase());

        signUpButton.onClick.AddListener(OnSignUpButtonClick);
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
            dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Pointer Click Detected - Simulating button press.");
        signUpButton.onClick.Invoke();
    }

    public void OnSignUpButtonClick()
    {
        string firstName = firstNameInput.text.Trim();
        string lastName = lastNameInput.text.Trim();
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (ValidateInputs(firstName, lastName, email, password))
        {
            StartCoroutine(SignUpUser(firstName, lastName, email, password));
        }
    }

    private bool ValidateInputs(string firstName, string lastName, string email, string password)
    {
        List<string> missingFields = new List<string>();

        if (string.IsNullOrWhiteSpace(firstName)) missingFields.Add("First Name");
        if (string.IsNullOrWhiteSpace(lastName)) missingFields.Add("Last Name");
        if (string.IsNullOrWhiteSpace(email)) missingFields.Add("Email");
        if (string.IsNullOrWhiteSpace(password)) missingFields.Add("Password");

        if (missingFields.Count > 0)
        {
            ShowError($"Please fill in: {string.Join(", ", missingFields)}");
            return false;
        }

        return true;
    }

    IEnumerator SignUpUser(string firstName, string lastName, string email, string password)
    {
        if (auth == null)
        {
            ShowError("Authentication service is not initialized.");
            yield break;
        }

        var signUpTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);
        yield return new WaitUntil(() => signUpTask.IsCompleted);

        if (signUpTask.IsFaulted || signUpTask.IsCanceled)
        {
            HandleSignUpError(signUpTask.Exception);
        }
        else
        {
            FirebaseUser newUser = auth.CurrentUser;
            if (newUser != null)
            {
                UserProfile profile = new UserProfile { DisplayName = $"{firstName} {lastName}" };
                var profileUpdateTask = newUser.UpdateUserProfileAsync(profile);
                yield return new WaitUntil(() => profileUpdateTask.IsCompleted);

                StartCoroutine(SaveUserToDatabase(newUser.UserId, firstName, lastName, email));
            }
        }
    }

    void HandleSignUpError(AggregateException exception)
    {
        Debug.LogError("Sign Up failed: " + exception);
        ShowError("An unknown error occurred.");
    }

    IEnumerator SaveUserToDatabase(string userId, string firstName, string lastName, string email)
    {
        Dictionary<string, object> userData = new Dictionary<string, object>
        {
            { "userId", userId },
            { "firstName", firstName },
            { "lastName", lastName },
            { "email", email },
            { "accountBalance", 1000 }
        };

        var dbTask = dbReference.Child("users").Child(userId).SetValueAsync(userData);
        yield return new WaitUntil(() => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.LogError("Failed to save user data: " + dbTask.Exception);
        }

        GoToLoginScene();
    }

    public void GoToLoginScene()
    {
        SceneManager.LoadScene("LoginScene");
    }

    void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = Color.red;
        }
        Debug.LogError(message);
    }
}