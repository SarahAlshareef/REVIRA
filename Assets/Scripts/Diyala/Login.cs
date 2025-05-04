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
using System.Collections.Generic; // Added for Dictionary in SetUserData call
using System; // Added for Exception handling

public class Login : MonoBehaviour
{
    public TMP_InputField emailInput, passwordInput;
    public Button loginButton, signUpButton;
    public TextMeshProUGUI errorText;

    private FirebaseAuth auth;
    private DatabaseReference dbReference; // Added dbReference for data fetching

    void Start()
    {
        Debug.Log("[DEBUG Login] Start");
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                dbReference = FirebaseDatabase.DefaultInstance.RootReference; // Initialize dbReference
                Debug.Log("[DEBUG Login] Firebase dependencies resolved. Auth and DB referenced.");
            }
            else
            {
                Debug.LogError("[DEBUG Login] Firebase dependency check failed: " + task.Exception);
                ShowError("Firebase failed to initialize.");
            }
        });

        loginButton?.onClick.AddListener(OnLoginButtonClick);
        signUpButton?.onClick.AddListener(() => SceneManager.LoadScene("SignUpScene"));

        // Check if user is already logged in on Start
        if (auth != null && auth.CurrentUser != null)
        {
            Debug.Log("[DEBUG Login] User already logged in. Attempting to load data and go to Store scene."); // Updated log
            LoadUserDataAndGoHome(auth.CurrentUser.UserId);
        }
        else
        {
            Debug.Log("[DEBUG Login] No user logged in on Start.");
        }
    }

    public void OnLoginButtonClick()
    {
        Debug.Log("[DEBUG Login] OnLoginButtonClick triggered");

        string email = emailInput?.text.Trim();
        string password = passwordInput?.text.Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Email and Password are required.");
            Debug.LogWarning("[DEBUG Login] Email or password input is empty.");
            return;
        }

        if (auth == null)
        {
            ShowError("Authentication service is not initiated.");
            Debug.LogError("[DEBUG Login] FirebaseAuth instance is null.");
            return;
        }

        Debug.Log($"[DEBUG Login] Attempting to sign in with email: {email}");
        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(loginTask =>
        {
            Debug.Log("[DEBUG Login] SignInWithEmailAndPasswordAsync task completed.");

            if (loginTask.IsFaulted || loginTask.IsCanceled)
            {
                Debug.LogError("[DEBUG Login] Login failed: " + loginTask.Exception);
                ShowError("Invalid email or password. Please try again.");
                return;
            }

            // Login successful!
            FirebaseUser user = loginTask.Result.User;
            if (user != null)
            {
                string userId = user.UserId;
                Debug.Log($"[DEBUG Login] Login successful! Firebase User ID: {userId}");

                // Proceed to load user data from the database and go to Store scene
                LoadUserDataAndGoHome(userId);
            }
            else
            {
                Debug.LogError("[DEBUG Login] Login task completed, but auth.CurrentUser is null.");
                ShowError("Login failed unexpectedly.");
            }
        });
    }

    // Function to load user data from DB and transition to Store scene
    private void LoadUserDataAndGoHome(string userId) // Function name kept for consistency, but it loads Store scene now
    {
        Debug.Log($"[DEBUG Login] Loading user data for user ID: {userId}");

        if (dbReference == null)
        {
            Debug.LogError("[DEBUG Login] Database reference is null. Cannot load user data.");
            ShowError("Database not ready. Try again.");
            return;
        }

        dbReference.Child("REVIRA").Child("Consumers").Child(userId)
            .GetValueAsync().ContinueWithOnMainThread(dbTask =>
            {
                Debug.Log("[DEBUG Login] GetValueAsync (user data) task completed.");

                if (dbTask.IsFaulted || dbTask.IsCanceled || !dbTask.Result.Exists)
                {
                    Debug.LogError("[DEBUG Login] Failed to load user data from DB: " + dbTask.Exception);
                    ShowError("Failed to load user data.");
                    return;
                }

                DataSnapshot snapshot = dbTask.Result;

                // Extract data from snapshot, providing default values if nodes don't exist
                string firstName = snapshot.Child("firstName").Value?.ToString() ?? "Not Added";
                string lastName = snapshot.Child("lastName").Value?.ToString() ?? "Not Added";
                string userEmail = snapshot.Child("email").Value?.ToString() ?? "Not Added";
                // Safely parse account balance
                float accountBalance = 0f;
                if (snapshot.Child("accountBalance").Value != null)
                {
                    if (!float.TryParse(snapshot.Child("accountBalance").Value.ToString(), out accountBalance))
                    {
                        Debug.LogWarning("[DEBUG Login] Could not parse accountBalance. Using default 0.");
                    }
                }
                string gender = snapshot.Child("gender").Exists ? snapshot.Child("gender").Value.ToString() : "Not Added";
                string phone = snapshot.Child("phoneNumber").Exists ? snapshot.Child("phoneNumber").Value.ToString() : "Not Added";

                // ADD THIS DEBUG LOG TO SEE THE DATA BEFORE SETTING USERMANAGER
                Debug.Log($"[DEBUG Login] Data loaded from DB: UserId={userId}, FirstName={firstName}, LastName={lastName}, Email={userEmail}, Balance={accountBalance}, Gender={gender}, Phone={phone}");


                // Set the user data in the UserManager Singleton
                if (UserManager.Instance != null)
                {
                    UserManager.Instance.SetUserData(userId, firstName, lastName, userEmail, accountBalance, gender, phone);
                    Debug.Log("[DEBUG Login] UserManager SetUserData called successfully.");

                    // Transition to the Store scene
                    SceneManager.LoadScene("Store"); // Changed from "HomeScene" to "Store"
                }
                else
                {
                    Debug.LogError("[DEBUG Login] UserManager.Instance is null! Cannot set user data.");
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
            errorText.gameObject.SetActive(true); // Ensure error text is visible
                                                  // Optional: Hide error after a few seconds
            CancelInvoke(nameof(HideError));
            Invoke(nameof(HideError), 5f);
        }
        Debug.LogError("[DEBUG Login] Showing Error: " + message);
    }

    void HideError()
    {
        if (errorText != null)
        {
            errorText.gameObject.SetActive(false);
            Debug.Log("[DEBUG Login] Error message hidden.");
        }
    }
}
