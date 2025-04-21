// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// Firebase
using Firebase.Database;
using Firebase.Extensions;
// C#
using System.Collections;
using System.Collections.Generic;


public class UpdateInformation : MonoBehaviour
{
    [Header("Panels")]
    public GameObject viewInformationPanel;
    public GameObject updateInformationPanel;

    [Header("Update Information Panel")]
    public TMP_InputField firstNameInput;
    public TMP_InputField lastNameInput;
    public TMP_InputField phoneInput;
    public Toggle maleToggle;
    public Toggle femaleToggle;

    [Header("Display Message")]
    public TextMeshProUGUI messageText;
    private Coroutine messageCoroutine;

    [Header("Update Information Buttons")]
    public Button updateInfoButton;
    public Button saveButton;
    public Button discardButton;

    private string userId;

    void Start()
    {
        userId = UserManager.Instance.UserId;

        phoneInput.characterLimit = 10;
        phoneInput.onValueChanged.AddListener(FilterPhoneNumber);

        updateInfoButton?.onClick.AddListener(ShowUpdatePanel);
        saveButton?.onClick.AddListener(SaveChanges);
        discardButton?.onClick.AddListener(LoadUpdateData);
    }
    public void ShowUpdatePanel()
    {
        viewInformationPanel.SetActive(false);
        updateInformationPanel.SetActive(true);
        LoadUpdateData();
    }
    public void LoadUpdateData()
    {
        firstNameInput.text = string.IsNullOrEmpty(UserManager.Instance.FirstName) ? "Not Added" : UserManager.Instance.FirstName;
        lastNameInput.text = string.IsNullOrEmpty(UserManager.Instance.LastName) ? "Not Added" : UserManager.Instance.LastName;
        phoneInput.text = string.IsNullOrEmpty(UserManager.Instance.PhoneNumber) ? "Not Added" : UserManager.Instance.PhoneNumber;

        string UserGender = (UserManager.Instance.Gender ?? "").ToLower();
        maleToggle.isOn = (UserGender == "male");
        femaleToggle.isOn = (UserGender == "female");

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
    void SaveChanges()
    {
        string newFirstName = firstNameInput.text.Trim();
        string newLastName = lastNameInput.text.Trim();
        string newPhone = phoneInput.text.Trim();
        string newGender = maleToggle.isOn ? "Male" : (femaleToggle.isOn ? "Female" : "");

        if (string.IsNullOrEmpty(newFirstName) || string.IsNullOrEmpty(newLastName) ||
            string.IsNullOrEmpty(newPhone) || string.IsNullOrEmpty(newGender))
        {
            ShowMessage("Please complete all fields before saving.", Color.red);
            return;
        }        
        UpdateUserData(newFirstName, newLastName, newPhone, newGender);
  
    }

    void UpdateUserData(string firstName, string lastName, string phone, string gender)
    {
        DatabaseReference userRef = FirebaseDatabase.DefaultInstance.RootReference.Child("REVIRA").Child("Consumers").Child(userId);

        var updates = new Dictionary<string, object>
                {
                    { "firstName", firstName },
                    { "lastName", lastName },
                    { "phoneNumber", phone },
                    { "gender", gender }
                };
        userRef.UpdateChildrenAsync(updates).ContinueWithOnMainThread(dbTask =>
        {
            if (dbTask.IsCompletedSuccessfully)
            {
                UserManager.Instance.UpdateFirstName(firstName);
                UserManager.Instance.UpdateLastName(lastName);
                UserManager.Instance.UpdatePhoneNumber(phone);
                UserManager.Instance.UpdateGender(gender);

                ShowMessage("Your information updated successfully.", Color.green);
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

            messageCoroutine = StartCoroutine(HideMessageAfterDelay(3f));
        }
    }
    IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        messageText.gameObject.SetActive(false);
    }
}

