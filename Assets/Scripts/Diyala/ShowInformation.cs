// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
// Firebase
using Firebase.Auth;


public class ShowInformation : MonoBehaviour
{
    [Header("General")]
    public Button closeProfileButton;
    public Button backToViewButton;
    public TextMeshProUGUI welcomeText;

    [Header("Panels")]
    public GameObject viewInformation;
    public GameObject updateInformation;

    [Header("View Information Panel")]
    public TextMeshProUGUI genderText;
    public TextMeshProUGUI firstNameText;
    public TextMeshProUGUI lastNameText;
    public TextMeshProUGUI emailText;
    public TextMeshProUGUI phoneText;
    public TextMeshProUGUI emailNoteText;

    private string userId;

    private void Awake()
    {
        viewInformation.SetActive(false);
        updateInformation.SetActive(false);
    }
    void Start()
    {
        userId = UserManager.Instance.UserId;
        welcomeText.text = $"Hi, {UserManager.Instance.FirstName} {UserManager.Instance.LastName}";
       
        closeProfileButton?.onClick.AddListener(CloseProfile);
        backToViewButton?.onClick.AddListener(ShowViewPanel);
    }
    public void CloseProfilePanel()
    {
        viewInformation.SetActive(false);
        updateInformation.SetActive(false);
    }
    public void ShowViewPanel()
    {
        viewInformation.SetActive(true);
        updateInformation.SetActive(false);
        LoadViewData();
    }
    public void LoadViewData()
    {
        string gender = string.IsNullOrEmpty(UserManager.Instance.Gender) ? "Not Added" : UserManager.Instance.Gender;
        string firstName = string.IsNullOrEmpty(UserManager.Instance.FirstName) ? "Not Added" : UserManager.Instance.FirstName;
        string lastName = string.IsNullOrEmpty(UserManager.Instance.LastName) ? "Not Added" : UserManager.Instance.LastName;
        string email = string.IsNullOrEmpty(UserManager.Instance.Email) ? "Not Added" : UserManager.Instance.Email;
        string phone = string.IsNullOrEmpty(UserManager.Instance.PhoneNumber) ? "Not Added" : UserManager.Instance.PhoneNumber;

        if (!FirebaseAuth.DefaultInstance.CurrentUser.IsEmailVerified)
        {
            emailNoteText.text = "Your new email is pending, waitng for verification.";
            emailNoteText.gameObject.SetActive(true);
        }
        else
        {
            emailNoteText.gameObject.SetActive(false);
        }

        genderText.text = gender;
        firstNameText.text = firstName;
        lastNameText.text = lastName;
        emailText.text = email;
        phoneText.text = phone;
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
}
