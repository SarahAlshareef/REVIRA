using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine.SceneManagement;

public class AddressBookManager : MonoBehaviour
{
    public Transform toggleParent;
    public GameObject addressTogglePrefab;
    public GameObject noAddressMessage;
    public Button addNewAddressButton;
    public GameObject addNewAddressButtonImage;
    public GameObject newAddressForm;
    public TMP_InputField addressNameInput, cityInput, districtInput, streetInput, buildingInput, phoneNumberInput;
    public TMP_Dropdown countryDropdown;
    public TextMeshProUGUI outsideErrorMessageText;
    public TextMeshProUGUI formErrorMessageText;
    public Button saveButton;
    public Button nextButton;

    private DatabaseReference dbReference;
    private List<Address> addressList = new();
    private const int maxAddresses = 3;

    private List<Toggle> allToggles = new(); // Track all toggles

    public static Address SelectedAddress { get; private set; }

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        StartCoroutine(WaitForUserIdAndLoad());

        addNewAddressButton.onClick.AddListener(ShowAddAddressForm);
        saveButton.onClick.AddListener(SaveNewAddress);
        nextButton.gameObject.SetActive(false); // Hidden initially

        nextButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("NextSceneName"); // Replace with your scene
        });
    }

    IEnumerator WaitForUserIdAndLoad()
    {
        while (string.IsNullOrEmpty(UserManager.Instance.UserId))
        {
            yield return null;
        }
        LoadAddresses();
    }

    void LoadAddresses()
    {
        ClearExistingToggles();
        allToggles.Clear();
        addressList.Clear();

        string userId = UserManager.Instance.UserId;
        dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("AddressBook").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                foreach (var addressSnap in task.Result.Children)
                {
                    Address address = JsonUtility.FromJson<Address>(addressSnap.GetRawJsonValue());
                    addressList.Add(address);
                    CreateToggle(address, addressSnap.Key);
                }
            }

            noAddressMessage.SetActive(addressList.Count == 0);
            nextButton.gameObject.SetActive(false); // Reset Next button

            if (addressList.Count >= maxAddresses)
            {
                addNewAddressButton.interactable = false;
                outsideErrorMessageText.text = "You’ve reached the maximum number of addresses. Delete one to add a new one.";
                if (addNewAddressButtonImage != null)
                    addNewAddressButtonImage.SetActive(false);
            }
            else
            {
                addNewAddressButton.interactable = true;
                outsideErrorMessageText.text = "";
                if (addNewAddressButtonImage != null)
                    addNewAddressButtonImage.SetActive(true);
            }
        });
    }

    void ClearExistingToggles()
    {
        foreach (Transform child in toggleParent)
        {
            Destroy(child.gameObject);
        }
    }

    void CreateToggle(Address address, string addressKey)
    {
        GameObject toggleGO = Instantiate(addressTogglePrefab, toggleParent);
        toggleGO.transform.Find("AddressText").GetComponent<TextMeshProUGUI>().text =
            $"{address.addressName}, {address.city}, {address.district}, {address.street}, {address.building}, {address.phoneNumber}";

        Toggle toggle = toggleGO.GetComponent<Toggle>();
        Button deleteButton = toggleGO.transform.Find("DeleteButton").GetComponent<Button>();

        // Prevent pre-selection
        toggle.isOn = false;

        // Assign Toggle Group
        ToggleGroup group = toggleParent.GetComponent<ToggleGroup>();
        if (group != null)
            toggle.group = group;

        allToggles.Add(toggle); // Track toggle

        toggle.onValueChanged.AddListener(isOn =>
        {
            if (isOn)
            {
                // Deselect all others
                foreach (var other in allToggles)
                {
                    if (other != toggle)
                        other.isOn = false;
                }

                SelectedAddress = address;
                nextButton.gameObject.SetActive(true); // Show Next button
            }
        });

        deleteButton.onClick.AddListener(() => DeleteAddress(addressKey));
    }

    void ShowAddAddressForm()
    {
        newAddressForm.SetActive(true);
        formErrorMessageText.text = "";
    }

    void SaveNewAddress()
    {
        string name = addressNameInput.text.Trim();
        string city = cityInput.text.Trim();
        string district = districtInput.text.Trim();
        string street = streetInput.text.Trim();
        string building = buildingInput.text.Trim();
        string phone = phoneNumberInput.text.Trim();
        string country = "Saudi Arabia";

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(city) || string.IsNullOrEmpty(district)
            || string.IsNullOrEmpty(street) || string.IsNullOrEmpty(building) || string.IsNullOrEmpty(phone))
        {
            formErrorMessageText.text = "Please fill in all fields.";
            return;
        }

        if (!phone.StartsWith("05"))
        {
            formErrorMessageText.text = "Phone number must start with '05'.";
            return;
        }

        if (phone.Length != 10 || !IsAllDigits(phone))
        {
            formErrorMessageText.text = "Phone number must be exactly 10 digits.";
            return;
        }

        Address newAddress = new(name, country, city, district, street, building, phone);
        int newIndex = addressList.Count + 1;
        string userId = UserManager.Instance.UserId;
        string path = $"REVIRA/Consumers/{userId}/AddressBook/Address{newIndex}";

        dbReference.Child(path).SetRawJsonValueAsync(JsonUtility.ToJson(newAddress)).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                newAddressForm.SetActive(false);
                ClearInputFields();
                LoadAddresses();
            }
        });
    }

    bool IsAllDigits(string str)
    {
        foreach (char c in str)
        {
            if (!char.IsDigit(c))
                return false;
        }
        return true;
    }

    void DeleteAddress(string addressKey)
    {
        string userId = UserManager.Instance.UserId;
        dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("AddressBook").Child(addressKey)
            .RemoveValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    ShiftAddressesAfterDeletion();
                }
            });
    }

    void ShiftAddressesAfterDeletion()
    {
        string userId = UserManager.Instance.UserId;
        dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("AddressBook").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            List<Address> remaining = new();

            if (task.IsCompleted && task.Result.Exists)
            {
                foreach (var addressSnap in task.Result.Children)
                {
                    Address address = JsonUtility.FromJson<Address>(addressSnap.GetRawJsonValue());
                    remaining.Add(address);
                }

                dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("AddressBook").RemoveValueAsync().ContinueWithOnMainThread(_ =>
                {
                    for (int i = 0; i < remaining.Count; i++)
                    {
                        string newKey = $"Address{i + 1}";
                        dbReference.Child("REVIRA").Child("Consumers").Child(userId)
                            .Child("AddressBook").Child(newKey)
                            .SetRawJsonValueAsync(JsonUtility.ToJson(remaining[i]));
                    }
                    LoadAddresses();
                });
            }
            else
            {
                LoadAddresses();
            }
        });
    }

    void ClearInputFields()
    {
        addressNameInput.text = "";
        cityInput.text = "";
        districtInput.text = "";
        streetInput.text = "";
        buildingInput.text = "";
        phoneNumberInput.text = "";
        formErrorMessageText.text = "";
    }
}
