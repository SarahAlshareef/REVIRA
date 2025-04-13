using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;

public class AddressUpdateViewManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform addressParent;
    public GameObject addressBarPrefab;
    public GameObject addNewAddressSection;
    public Button addNewAddressButton;
    public TMP_InputField addressNameInput, cityInput, districtInput, streetInput, buildingInput, phoneNumberInput;
    public TMP_Dropdown countryDropdown;
    public TextMeshProUGUI errorMessageText;

    private DatabaseReference dbRef;
    private List<GameObject> addressBars = new();
    private string userId;

    private const int maxAddresses = 3;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;

        LoadAddresses();
        addNewAddressButton.onClick.AddListener(OnAddNewAddress);
    }

    void LoadAddresses()
    {
        dbRef.Child("REVIRA/Consumers/" + userId + "/Addresses").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                foreach (Transform child in addressParent)
                    Destroy(child.gameObject);

                addressBars.Clear();

                DataSnapshot snapshot = task.Result;
                foreach (DataSnapshot child in snapshot.Children)
                {
                    Address address = JsonUtility.FromJson<Address>(child.GetRawJsonValue());
                    GameObject bar = Instantiate(addressBarPrefab, addressParent);

                    // Fill address fields
                    bar.transform.Find("AddressName").GetComponent<TextMeshProUGUI>().text = address.addressName;
                    bar.transform.Find("City").GetComponent<TextMeshProUGUI>().text = address.city;

                    // Add delete button logic
                    Button deleteBtn = bar.transform.Find("DeleteButton").GetComponent<Button>();
                    string key = child.Key;
                    deleteBtn.onClick.AddListener(() => DeleteAddress(key));

                    addressBars.Add(bar);
                }

                addNewAddressButton.interactable = addressBars.Count < maxAddresses;
            }
        });
    }

    void OnAddNewAddress()
    {
        if (string.IsNullOrEmpty(addressNameInput.text) || string.IsNullOrEmpty(cityInput.text) ||
            string.IsNullOrEmpty(districtInput.text) || string.IsNullOrEmpty(streetInput.text) ||
            string.IsNullOrEmpty(buildingInput.text) || string.IsNullOrEmpty(phoneNumberInput.text))
        {
            errorMessageText.text = "Please fill all fields.";
            return;
        }

        Address newAddress = new()
        {
            addressName = addressNameInput.text,
            country = countryDropdown.options[countryDropdown.value].text,
            city = cityInput.text,
            district = districtInput.text,
            street = streetInput.text,
            building = buildingInput.text,
            phoneNumber = phoneNumberInput.text
        };

        string key = dbRef.Child("REVIRA/Consumers/" + userId + "/Addresses").Push().Key;
        string json = JsonUtility.ToJson(newAddress);

        dbRef.Child("REVIRA/Consumers/" + userId + "/Addresses").Child(key).SetRawJsonValueAsync(json).ContinueWithOnMainThread(saveTask =>
        {
            if (saveTask.IsCompleted)
            {
                ClearInputs();
                LoadAddresses();
                errorMessageText.text = "";
            }
            else
            {
                errorMessageText.text = "Failed to add address.";
            }
        });
    }

    void DeleteAddress(string key)
    {
        dbRef.Child("REVIRA/Consumers/" + userId + "/Addresses").Child(key).RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                LoadAddresses();
            }
            else
            {
                errorMessageText.text = "Failed to delete address.";
            }
        });
    }

    void ClearInputs()
    {
        addressNameInput.text = "";
        cityInput.text = "";
        districtInput.text = "";
        streetInput.text = "";
        buildingInput.text = "";
        phoneNumberInput.text = "";
        countryDropdown.value = 0;
    }
}
