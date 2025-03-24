using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;

public class AddressBookManager : MonoBehaviour
{
    public Transform toggleParent;
    public GameObject addressTogglePrefab;
    public GameObject noAddressMessage;
    public Button addNewAddressButton;
    public GameObject newAddressForm;
    public TMP_InputField addressNameInput, cityInput, districtInput, streetInput, buildingInput, phoneNumberInput;
    public TMP_Dropdown countryDropdown;
    public TextMeshProUGUI outsideErrorMessageText; // For max addresses
    public TextMeshProUGUI formErrorMessageText;    // For empty fields in form
    public Button saveButton;

    private DatabaseReference dbReference;
    private List<Address> addressList = new();
    private const int maxAddresses = 3;

    public static Address SelectedAddress { get; private set; }

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        StartCoroutine(WaitForUserIdAndLoad());

        addNewAddressButton.onClick.AddListener(ShowAddAddressForm);
        saveButton.onClick.AddListener(SaveNewAddress);
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
        string userId = UserManager.Instance.UserId;
        dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("AddressBook").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                ClearExistingToggles();
                addressList.Clear();

                foreach (var addressSnap in task.Result.Children)
                {
                    if (addressSnap.Key == "empty") continue; // Ignore dummy key

                    Address address = JsonUtility.FromJson<Address>(addressSnap.GetRawJsonValue());
                    addressList.Add(address);
                    CreateToggle(address, addressSnap.Key);
                }
                noAddressMessage.SetActive(addressList.Count == 0);

                // Disable Add button + show outside error message
                if (addressList.Count >= maxAddresses)
                {
                    addNewAddressButton.interactable = false;
                    outsideErrorMessageText.text = "You’ve reached the maximum number of addresses. Delete one to add a new one.";
                }
                else
                {
                    addNewAddressButton.interactable = true;
                    outsideErrorMessageText.text = "";
                }
            }
            else
            {
                ClearExistingToggles();
                noAddressMessage.SetActive(true);
                addNewAddressButton.interactable = true;
                outsideErrorMessageText.text = "";
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

        toggle.onValueChanged.AddListener(isOn =>
        {
            if (isOn)
                SelectedAddress = address;
        });

        deleteButton.onClick.AddListener(() => DeleteAddress(addressKey));
    }

    void ShowAddAddressForm()
    {
        newAddressForm.SetActive(true);
        formErrorMessageText.text = ""; // Clear form error when opening
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

        Address newAddress = new(name, country, city, district, street, building, phone);
        int newIndex = addressList.Count + 1;
        string userId = UserManager.Instance.UserId;
        string path = $"REVIRA/Consumers/{userId}/AddressBook/Address{newIndex}";

        // Remove dummy "empty" key if exists
        dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("AddressBook").Child("empty").RemoveValueAsync();

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

    void DeleteAddress(string addressKey)
    {
        string userId = UserManager.Instance.UserId;
        dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("AddressBook").Child(addressKey)
            .RemoveValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    CheckAndShiftAddresses();
                }
            });
    }

    void CheckAndShiftAddresses()
    {
        string userId = UserManager.Instance.UserId;

        dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("AddressBook").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                int realCount = 0;
                foreach (var snap in task.Result.Children)
                {
                    if (snap.Key != "empty")
                        realCount++;
                }

                if (task.Result.Exists && realCount > 0)
                {
                    ShiftAddressesAfterDeletion(task.Result);
                }
                else
                {
                    dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("AddressBook").Child("empty").SetValueAsync("true");

                    ClearExistingToggles();
                    addressList.Clear();
                    noAddressMessage.SetActive(true);
                    addNewAddressButton.interactable = true;
                    outsideErrorMessageText.text = "";
                }
            }
        });
    }

    void ShiftAddressesAfterDeletion(DataSnapshot snapshot)
    {
        string userId = UserManager.Instance.UserId;
        addressList.Clear();

        List<Address> remaining = new();

        foreach (var addressSnap in snapshot.Children)
        {
            if (addressSnap.Key == "empty") continue;

            Address address = JsonUtility.FromJson<Address>(addressSnap.GetRawJsonValue());
            remaining.Add(address);
        }

        foreach (var addressSnap in snapshot.Children)
        {
            if (addressSnap.Key == "empty") continue;
            dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("AddressBook").Child(addressSnap.Key).RemoveValueAsync();
        }

        for (int i = 0; i < remaining.Count; i++)
        {
            string newKey = $"Address{i + 1}";
            dbReference.Child("REVIRA").Child("Consumers").Child(userId)
                .Child("AddressBook").Child(newKey)
                .SetRawJsonValueAsync(JsonUtility.ToJson(remaining[i]));
        }

        LoadAddresses();
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
