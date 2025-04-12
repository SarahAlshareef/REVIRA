// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;
//Firebase
using Firebase.Database;
using Firebase.Extensions;

public class PersonalInformation : MonoBehaviour
{
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
    public TextMeshProUGUI welcomeText;

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

        ShowViewPanel();
        LoadViewData();
        LoadUpdateData();

        updateInfoButton?.onClick.AddListener(ShowUpdatePanel);
        saveButton?.onClick.AddListener(SaveChanges);
        discardButton?.onClick.AddListener(LoadUpdateData);
        backButton?.onClick.AddListener(ShowViewPanel);
    }
    void ShowViewPanel()
    {
        viewInformation.SetActive(true);
        updateInformation.SetActive(false);
    }
    void ShowUpdatePanel()
    {
        viewInformation.SetActive(false);
        updateInformation.SetActive(true);
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

        welcomeText.text = $"Hi, {firstName} {lastName}";
    }
    void LoadUpdateData()
    {
        firstNameInput.text = UserManager.Instance.FirstName;
        lastNameInput.text = UserManager.Instance.LastName;
        emailInput.text = UserManager.Instance.Email;
        phoneInput.text = UserManager.Instance.PhoneNumber;

        string UserGender = UserManager.Instance.Gender.ToLower();
        maleToggle.isOn = UserGender == "male";
        femaleToggle.isOn = UserGender == "female";
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
            ShowMessage("Please fill in all fields.", Color.red);
            return;
        }

        DatabaseReference userRef = FirebaseDatabase.DefaultInstance.RootReference.Child("REVIRA").Child("Consumers").Child(userId);

        userRef.Child("firstName").SetValueAsync(newFirstName);
        userRef.Child("lastName").SetValueAsync(newLastName);
        userRef.Child("email").SetValueAsync(newEmail);
        userRef.Child("phoneNumber").SetValueAsync(newPhone);

        userRef.Child("gender").SetValueAsync(newGender).ContinueWithOnMainThread(task =>
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
        });
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