using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Auth;
using Firebase.Extensions;
using System.Collections.Generic;

public class AddressBookController : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField addressNameInput, cityInput, streetInput, buildingInput, zipCodeInput, phoneNumberInput;

    [Header("UI Elements")]
    public Button addAddressButton;
    public TMP_Dropdown addressDropdown;
    public TextMeshProUGUI statusText;

    private DatabaseReference dbReference;
    private string userId;
    private Dictionary<string, Address> addressBook = new Dictionary<string, Address>();
    private List<string> addressIDs = new List<string>();

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;

        addAddressButton?.onClick.AddListener(AddNewAddress);

        LoadAddressBook();
    }

    void AddNewAddress()
    {
        if (string.IsNullOrWhiteSpace(addressNameInput.text) ||
            string.IsNullOrWhiteSpace(cityInput.text) ||
            string.IsNullOrWhiteSpace(streetInput.text) ||
            string.IsNullOrWhiteSpace(buildingInput.text) ||
            string.IsNullOrWhiteSpace(zipCodeInput.text) ||
            string.IsNullOrWhiteSpace(phoneNumberInput.text))
        {
            ShowStatus("Please fill all fields.");
            return;
        }

        string addressID = dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("addressBook").Push().Key;
        Address newAddress = new Address(
            addressNameInput.text.Trim(),
            cityInput.text.Trim(),
            streetInput.text.Trim(),
            buildingInput.text.Trim(),
            zipCodeInput.text.Trim(),
            phoneNumberInput.text.Trim()
        );

        string json = JsonUtility.ToJson(newAddress);

        dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("addressBook").Child(addressID)
            .SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                {
                    addressBook[addressID] = newAddress;
                    addressIDs.Add(addressID);
                    UpdateDropdown();
                    SaveAddressBookToUserManager();
                    ShowStatus("Address added successfully!", true);
                }
                else
                {
                    ShowStatus("Error adding address.");
                }
            });
    }

    void LoadAddressBook()
    {
        dbReference.Child("REVIRA").Child("Consumers").Child(userId).Child("addressBook").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                addressBook.Clear();
                addressIDs.Clear();

                foreach (DataSnapshot addressSnapshot in snapshot.Children)
                {
                    string id = addressSnapshot.Key;
                    Address address = JsonUtility.FromJson<Address>(addressSnapshot.GetRawJsonValue());
                    addressBook[id] = address;
                    addressIDs.Add(id);
                }

                UpdateDropdown();
                SaveAddressBookToUserManager();
            }
        });
    }

    void UpdateDropdown()
    {
        addressDropdown.ClearOptions();
        List<string> options = new List<string> { "Select Address" };
        foreach (string id in addressIDs)
        {
            options.Add(addressBook[id].addressName);
        }
        addressDropdown.AddOptions(options);
        addressDropdown.SetValueWithoutNotify(0);
        addressDropdown.RefreshShownValue();

        addressDropdown.onValueChanged.AddListener(delegate { StoreSelectedAddress(addressDropdown.value); });
    }

    void StoreSelectedAddress(int index)
    {
        if (index == 0)
        {
            UserManager.Instance.SetSelectedAddress(null);
            return;
        }

        string selectedID = addressIDs[index - 1]; // "Select Address" is index 0
        Address selectedAddress = addressBook[selectedID];
        UserManager.Instance.SetSelectedAddress(selectedAddress);
        Debug.Log($"Selected Address: {selectedAddress.addressName}");
    }

    void SaveAddressBookToUserManager()
    {
        UserManager.Instance.SetAddressBook(addressBook);
    }

    void ShowStatus(string message, bool success = false)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = success ? Color.green : Color.red;
        }
    }
}
