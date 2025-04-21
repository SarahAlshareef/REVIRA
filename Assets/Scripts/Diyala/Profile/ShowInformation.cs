// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;


public class ShowInformation : MonoBehaviour
{
    [Header("General")]
    public Button closeProfileButton;
    public TextMeshProUGUI welcomeText;

    [Header("Panels")]
    public GameObject viewInformationPanel;
    public GameObject updateInformationPanel;

    [Header("View Information Panel")]
    public TextMeshProUGUI genderText;
    public TextMeshProUGUI firstNameText;
    public TextMeshProUGUI lastNameText;
    public TextMeshProUGUI emailText;
    public TextMeshProUGUI phoneText;

    [Header("Update Information Panel")]
    public Button backToViewButton;

    [Header("Adresses")]
    public AddressDisplayOnly addressScript;

    private string userId;

    void Awake()
    {
        viewInformationPanel.SetActive(false);
        updateInformationPanel.SetActive(false);
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
        viewInformationPanel.SetActive(false);
        updateInformationPanel.SetActive(false);
    }

    public void ShowViewPanel()
    {
        viewInformationPanel.SetActive(true);
        updateInformationPanel.SetActive(false);
        LoadViewData();
        addressScript?.LoadAddresses();
    }

    public void LoadViewData()
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
