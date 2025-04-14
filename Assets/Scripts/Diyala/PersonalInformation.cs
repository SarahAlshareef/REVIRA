// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
// Firebase
using Firebase.Database;
using Firebase.Extensions;
// C#
using System.Collections.Generic;

public class PersonalInformation : MonoBehaviour
{
    [Header("General")]
    public Button closeProfileButton;
    public TextMeshProUGUI welcomeText;

    [Header("Panels")]
    public GameObject viewInformation;
    public GameObject updateInformation;

    [Header("View Information Panel")]
    public Button updateInfoButton;
    public TextMeshProUGUI genderText;
    public TextMeshProUGUI firstNameText;
    public TextMeshProUGUI lastNameText;
    public TextMeshProUGUI emailText;
    public TextMeshProUGUI phoneText;

    [Header("Update Information Panel")]
    public TMP_InputField firstNameInput;
    public TMP_InputField lastNameInput;
    public TMP_InputField emailInput;
    public TMP_InputField phoneInput;
    public Toggle maleToggle;
    public Toggle femaleToggle;
    public Button saveButton;
    public Button discardButton;
    public Button backButton;
    public TextMeshProUGUI messageText;

    private string userId;

    void Start()
    {
        userId = UserManager.Instance.UserId;
        welcomeText.text = $"Hi, {UserManager.Instance.FirstName} {UserManager.Instance.LastName}";

        phoneInput.characterLimit = 10;
        phoneInput.onValueChanged.AddListener(FilterPhoneNumber);

        viewInformation.SetActive(false);
        updateInformation.SetActive(false);

        updateInfoButton?.onClick.AddListener(ShowUpdatePanel);
        saveButton?.onClick.AddListener(SaveChanges);
        discardButton?.onClick.AddListener(LoadUpdateData);
        backButton?.onClick.AddListener(ShowViewPanel);

        closeProfileButton?.onClick.AddListener(CloseProfile);
    }
    public void ShowProfile()
    {     
        ShowViewPanel();        
    }
    public void CloseProfilePanel()
    {
        viewInformation.SetActive(false);
        updateInformation.SetActive(false);
    }
    void ShowViewPanel()
    {
        viewInformation.SetActive(true);
        updateInformation.SetActive(false);
        LoadViewData();
    }
    void ShowUpdatePanel()
    {
        viewInformation.SetActive(false);
        updateInformation.SetActive(true);
        LoadUpdateData();
    }
    void LoadViewData()
    {
        string gender = string.IsNullOrEmpty(UserManager.Instance.Gender) ? "Not Added" : UserManager.Instance.Gender;
        string firstName = string.IsNullOrEmpty(UserManager.Instance.FirstName) ? "Not Added" : UserManager.Instance.FirstName;
        string lastName = string.IsNullOrEmpty(UserManager.Instance.LastName) ? "Not Added" : UserManager.Instance.LastName;
        string email = string.IsNullOrEmpty(UserManager.Instance.Email) ? "Not Added" : UserManager.Instance.Email;
        string phone = string.IsNullOrEmpty(UserManager.Instance.PhoneNumber) ? "Not Added" : UserManager.Instance.PhoneNumber;

        genderText.text = gender;
        firstNameText.text = firstName;
        lastNameText.text = lastName;
        emailText.text = email;
        phoneText.text = phone;
    }
    void LoadUpdateData()
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

        foreach (char c in input) {
            if (char.IsDigit(c)) digitsOnly += c;
        }
        if (phoneInput.text != digitsOnly )
            phoneInput.text = digitsOnly;
    }
    void SaveChanges()
    {
        string newFirstName = firstNameInput.text.Trim();
        string newLastName = lastNameInput.text.Trim();
        string newEmail = emailInput.text.Trim();
        string newPhone = phoneInput.text.Trim();
        string newGender = maleToggle.isOn ? "Male" : (femaleToggle.isOn ? "Female" : "");

        if ( string.IsNullOrEmpty(newFirstName) || string.IsNullOrEmpty(newLastName) || string.IsNullOrEmpty(newEmail) || 
            string.IsNullOrEmpty(newPhone) || string.IsNullOrEmpty(newGender))
        {
            ShowMessage("Please complete all fields before saving.", Color.red);
            return;
        }

        DatabaseReference userRef = FirebaseDatabase.DefaultInstance.RootReference.Child("REVIRA").Child("Consumers").Child(userId);

        var updates = new Dictionary<string, object>
    {
        { "firstName", newFirstName },
        { "lastName", newLastName },
        { "email", newEmail },
        { "phoneNumber", newPhone },
        { "gender", newGender }
    };

        userRef.UpdateChildrenAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                UserManager.Instance.UpdateFirstName(newFirstName);
                UserManager.Instance.UpdateLastName(newLastName);
                UserManager.Instance.UpdateEmail(newEmail);
                UserManager.Instance.UpdatePhoneNumber(newPhone);
                UserManager.Instance.UpdateGender(newGender);

                LoadViewData(); 
                ShowMessage("Your information has been updated successfully.", Color.green);
            }
            else
            {
                ShowMessage("Failed to update information. Please try again.", Color.red);
            }
        });
    }

    public void CloseProfile()
    {
        if (!string.IsNullOrEmpty(SceneTracker.Instance.PreviousSceneName))
        {
            SceneManager.LoadScene(SceneTracker.Instance.PreviousSceneName);
        }
        else
        {
            Debug.LogWarning("No previous scene stored.");
        }
    }

    void ShowMessage(string message, Color color)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = color;
        }
    }
}