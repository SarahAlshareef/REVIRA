// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// Firebase
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
// C#
using System.Collections;
using System.Collections.Generic;


public class UpdateInformation : MonoBehaviour
{
    [Header("Panels")]
    public GameObject viewInformation;
    public GameObject updateInformation;

    [Header("Update Information Panel")]
    public TMP_InputField firstNameInput;
    public TMP_InputField lastNameInput;
    public TMP_InputField emailInput;
    public TMP_InputField phoneInput;
    public Toggle maleToggle;
    public Toggle femaleToggle;

    [Header("Display Message")]
    public TextMeshProUGUI messageText;
    private Coroutine messageCoroutine;

    [Header("Email change Authentication")]
    public GameObject passwordPanel;
    public TMP_InputField passwordInput;

    [Header("Update Information Buttons")]
    public Button updateInfoButton;
    public Button saveButton;
    public Button discardButton;

    private string userId;

    void Start()
    {
        userId = UserManager.Instance.UserId;

        passwordPanel.SetActive(false);
        emailInput.onValueChanged.AddListener(OnEmailChange);

        phoneInput.characterLimit = 10;
        phoneInput.onValueChanged.AddListener(FilterPhoneNumber);

        updateInfoButton?.onClick.AddListener(ShowUpdatePanel);
        saveButton?.onClick.AddListener(SaveChanges);
        discardButton?.onClick.AddListener(LoadUpdateData);
    }
    public void ShowUpdatePanel()
    {
        viewInformation.SetActive(false);
        updateInformation.SetActive(true);
        LoadUpdateData();
    }
    public void LoadUpdateData()
    {
        firstNameInput.text = UserManager.Instance.FirstName;
        lastNameInput.text = UserManager.Instance.LastName;
        emailInput.text = UserManager.Instance.Email;
        phoneInput.text = UserManager.Instance.PhoneNumber;

        string UserGender = (UserManager.Instance.Gender ?? "").ToLower();
        maleToggle.isOn = UserGender == "male";
        femaleToggle.isOn = UserGender == "female";

        maleToggle.onValueChanged.RemoveAllListeners();
        femaleToggle.onValueChanged.RemoveAllListeners();

        maleToggle.onValueChanged.AddListener((isOn) =>
        {
            if (isOn) femaleToggle.isOn = false;
        });
        femaleToggle.onValueChanged.AddListener((isOn) =>
        {
            if (isOn) maleToggle.isOn = false;
        });
    }
    void FilterPhoneNumber(string input)
    {
        string digitsOnly = "";

        foreach (char c in input)
        {
            if (char.IsDigit(c)) digitsOnly += c;
        }
        if (phoneInput.text != digitsOnly)
            phoneInput.text = digitsOnly;
    }
    void OnEmailChange (string newEmail)
    {
        if (newEmail.Trim() != UserManager.Instance.Email)
        {
            passwordPanel.SetActive(true); 
        }
        else
        {
            passwordPanel.SetActive(false);
            passwordInput.text = "";
        }
    }
    bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch { return false; }
    }
    void SaveChanges()
    {
        string newFirstName = firstNameInput.text.Trim();
        string newLastName = lastNameInput.text.Trim();
        string newEmail = emailInput.text.Trim();
        string newPhone = phoneInput.text.Trim();
        string newGender = maleToggle.isOn ? "Male" : (femaleToggle.isOn ? "Female" : "");

        if (string.IsNullOrEmpty(newFirstName) || string.IsNullOrEmpty(newLastName) || string.IsNullOrEmpty(newEmail) ||
            string.IsNullOrEmpty(newPhone) || string.IsNullOrEmpty(newGender))
        {
            ShowMessage("Please complete all fields before saving.", Color.red);
            return;
        }
        if (!IsValidEmail(newEmail))
        {
            ShowMessage("Please enter a valid email address.", Color.red);
            return;
        }
        if (newEmail != UserManager.Instance.Email)
        {
            string password = passwordInput.text.Trim();
            if (string.IsNullOrEmpty(password))
            {
                ShowMessage("Please enter your password to update the email.", Color.red);
                return;
            }

            var credential = EmailAuthProvider.GetCredential(UserManager.Instance.Email, password);

            FirebaseAuth.DefaultInstance.CurrentUser.ReauthenticateAsync(credential).ContinueWithOnMainThread(reAuthTask =>
            {
                if ( reAuthTask.IsCompleted && !reAuthTask.IsFaulted)
                {
                    FirebaseAuth.DefaultInstance.CurrentUser.UpdateEmailAsync(newEmail).ContinueWithOnMainThread(authTask =>
                    {
                        if (authTask.IsCompleted && !authTask.IsFaulted)
                        {
                            UpdateUserData(newFirstName, newLastName, newEmail, newPhone, newGender, "Information updated successfully. Your new email will be used to log in.");
                        }
                        else
                        {
                            ShowMessage("Failed to update email. It may already be in use.", Color.red);
                        }
                    });
                }
                else
                {
                    ShowMessage("Reauthentication failed. Please check your password.", Color.red);
                }
            });
        }
        else
        {
            UpdateUserData(newFirstName, newLastName, newEmail, newPhone, newGender, "Your information updated successfully.");
        }
    }
    void UpdateUserData(string firstName, string lastName, string email, string phone, string gender, string successMessage)
    {
        DatabaseReference userRef = FirebaseDatabase.DefaultInstance.RootReference.Child("REVIRA").Child("Consumers").Child(userId);

        var updates = new Dictionary<string, object>
                {
                    { "firstName", firstName },
                    { "lastName", lastName },
                    { "email", email },
                    { "phoneNumber", phone },
                    { "gender", gender }
                };
        userRef.UpdateChildrenAsync(updates).ContinueWithOnMainThread(dbTask =>
        {
            if (dbTask.IsCompleted)
            {
                UserManager.Instance.UpdateFirstName(firstName);
                UserManager.Instance.UpdateLastName(lastName);
                UserManager.Instance.UpdateEmail(email);
                UserManager.Instance.UpdatePhoneNumber(phone);
                UserManager.Instance.UpdateGender(gender);

                ShowMessage(successMessage, Color.green);
            }
            else
            {
                ShowMessage("Failed to update information. Please try again.", Color.red);
            }
        });
    }
    void ShowMessage(string message, Color color)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = color;
            messageText.gameObject.SetActive(true);

            if (messageCoroutine != null)
                StopCoroutine(messageCoroutine);

            messageCoroutine = StartCoroutine(HideMessageAfterDelay(5f));
        }
    }
    IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        messageText.gameObject.SetActive(false);
    }
}

