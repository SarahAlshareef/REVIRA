using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AddressUpdateManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform addressListParent;
    public GameObject addressBarPrefab, noAddressMessage, addNewAddressForm;
    public Button addNewAddressButton, discardButton, saveButton;
    public TMP_InputField addressNameInput, cityInput, districtInput, streetInput, buildingInput, phoneNumberInput;
    public TMP_Dropdown countryDropdown;
    public TextMeshProUGUI errorMessageText, confirmationMessage;

    private DatabaseReference dbRef;
    private string userId;
    private const int maxAddresses = 3;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        StartCoroutine(WaitForUserIdAndLoad());

        addNewAddressButton.onClick.AddListener(() =>
        {
            bool isActive = addNewAddressForm.activeSelf;
            addNewAddressForm.SetActive(!isActive);
            if (isActive) ClearInputs();
            errorMessageText.text = "";
        });

        discardButton.onClick.AddListener(() =>
        {
            ClearInputs();
            LoadAddresses();
        });

        saveButton.onClick.AddListener(() =>
        {
            confirmationMessage.text = "Changes saved successfully.";
        });

        confirmationMessage.text = "";
    }

    IEnumerator WaitForUserIdAndLoad()
    {
        while (string.IsNullOrEmpty(UserManager.Instance.UserId)) yield return null;
        userId = UserManager.Instance.UserId;
        LoadAddresses();
    }

    void LoadAddresses()
    {
        foreach (Transform child in addressListParent)
            Destroy(child.gameObject);

        dbRef.Child("REVIRA/Consumers/" + userId + "/AddressBook").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                int index = 1;
                foreach (var snap in task.Result.Children)
                {
                    Address address = JsonUtility.FromJson<Address>(snap.GetRawJsonValue());

                    GameObject go = Instantiate(addressBarPrefab, addressListParent);
                    go.transform.Find("AddressText").GetComponent<TextMeshProUGUI>().text =
                        $"{address.addressName}, {address.city}, {address.district}, {address.street}, {address.building}, {address.phoneNumber}";

                    Button deleteBtn = go.transform.Find("DeleteButton").GetComponent<Button>();
                    string key = snap.Key;
                    deleteBtn.onClick.AddListener(() => DeleteAddress(key));
                    index++;
                }

                noAddressMessage.SetActive(task.Result.ChildrenCount == 0);
                addNewAddressButton.interactable = task.Result.ChildrenCount < maxAddresses;
            }
        });
    }

    void DeleteAddress(string key)
    {
        dbRef.Child("REVIRA/Consumers/" + userId + "/AddressBook").Child(key).RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
                LoadAddresses();
        });
    }

    public void AddNewAddress()
    {
        if (string.IsNullOrEmpty(addressNameInput.text) || string.IsNullOrEmpty(cityInput.text) ||
            string.IsNullOrEmpty(districtInput.text) || string.IsNullOrEmpty(streetInput.text) ||
            string.IsNullOrEmpty(buildingInput.text) || string.IsNullOrEmpty(phoneNumberInput.text))
        {
            errorMessageText.text = "Please fill in all fields.";
            return;
        }

        if (!phoneNumberInput.text.StartsWith("05") || phoneNumberInput.text.Length != 10 || !phoneNumberInput.text.All(char.IsDigit))
        {
            errorMessageText.text = "Phone number must start with '05' and be 10 digits.";
            return;
        }

        dbRef.Child("REVIRA/Consumers/" + userId + "/AddressBook").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                int count = (int)task.Result.ChildrenCount;
                if (count >= maxAddresses)
                {
                    errorMessageText.text = "You’ve reached the maximum number of addresses.";
                    return;
                }

                string key = "Address" + (count + 1);
                Address newAddr = new(
                    addressNameInput.text.Trim(),
                    countryDropdown.options[countryDropdown.value].text,
                    cityInput.text.Trim(),
                    districtInput.text.Trim(),
                    streetInput.text.Trim(),
                    buildingInput.text.Trim(),
                    phoneNumberInput.text.Trim()
                );

                dbRef.Child("REVIRA/Consumers/" + userId + "/AddressBook").Child(key)
                    .SetRawJsonValueAsync(JsonUtility.ToJson(newAddr))
                    .ContinueWithOnMainThread(_ =>
                    {
                        ClearInputs();
                        addNewAddressForm.SetActive(false);
                        LoadAddresses();
                    });
            }
        });
    }

    void ClearInputs()
    {
        addressNameInput.text = cityInput.text = districtInput.text =
        streetInput.text = buildingInput.text = phoneNumberInput.text = "";
    }
}
