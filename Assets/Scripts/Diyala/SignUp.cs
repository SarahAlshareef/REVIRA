using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;

public class SignUp : MonoBehaviour
{
    [Header("UI Elements")]
    public InputField firstNameInput;
    public InputField lastNameInput;
    public InputField emailInput;
    public InputField passwordInput;
    public Button signUpButton;
    public Button loginButton;

    private FirebaseAuth auth;

    void Start()
    {
        // Initialize Firebase Authentication
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            auth = FirebaseAuth.DefaultInstance;
        });

        signUpButton.onClick.AddListener(OnSignUpButtonClick);
        loginButton.onClick.AddListener(GoToLoginScene);
    }

    public void OnSignUpButtonClick()
    {
        string firstName = firstNameInput.text;
        string lastName = lastNameInput.text;
        string email = emailInput.text;
        string password = passwordInput.text;

        SignUpUser(firstName, lastName, email, password);
    }

    private void SignUpUser(string firstName, string lastName, string email, string password)
    {
        // Ensure all fields are filled
        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
        {
            Debug.LogError("First Name and Last Name are required.");
            return;
        }
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Debug.LogError("Email and Password are required.");
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("Sign Up Failed: " + task.Exception);
            }
            else
            {
                Debug.Log("Sign Up Successful!");
                SceneManager.LoadScene("HomeScene"); // Navigate to HomeScene
            }
        });
    }

    public void GoToLoginScene()
    {
        SceneManager.LoadScene("LoginScene"); // Navigate to LoginScene
    }
}