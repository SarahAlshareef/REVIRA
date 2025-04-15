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

    public Button addNewAddressButton, discardButton, saveButton, confirmAddButton, backButton;

    public TMP_InputField addressNameInput, cityInput, districtInput, streetInput, buildingInput, phoneNumberInput;
    public TMP_Dropdown countryDropdown;
    public TextMeshProUGUI errorMessageText, confirmationMessage;

    [Header("UI Panels")]
    public GameObject addressUpdatePanel;
    public GameObject profileDetailsPanel;

    private DatabaseReference dbRef;
    private string userId;

    private List<Address> workingList = new();
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

        confirmAddButton.onClick.AddListener(AddNewAddress);

        discardButton.onClick.AddListener(() =>
        {
            ClearInputs();
            confirmationMessage.text = "";
            errorMessageText.text = "";
            LoadFromFirebase();
        });

        saveButton.onClick.AddListener(() =>
        {
            SaveToFirebase();
        });

        backButton.onClick.AddListener(OnBackButtonPressed);

        confirmationMessage.text = "";
        errorMessageText.text = "";
    }

    IEnumerator WaitForUserIdAndLoad()
    {
        while (string.IsNullOrEmpty(UserManager.Instance.UserId)) yield return null;
        userId = UserManager.Instance.UserId;
        LoadFromFirebase();
    }

    void LoadFromFirebase()
    {
        dbRef.Child("REVIRA/Consumers/" + userId + "/AddressBook").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            workingList.Clear();

            if (task.IsCompleted && task.Result.Exists)
            {
                foreach (var snap in task.Result.Children)
                {
                    Address address = JsonUtility.FromJson<Address>(snap.GetRawJsonValue());
                    workingList.Add(address);
                }
            }

            RebuildUIFromWorkingList();
        });
    }

    void RebuildUIFromWorkingList()
    {
        foreach (Transform child in addressListParent)
            Destroy(child.gameObject);

        for (int i = 0; i < workingList.Count; i++)
        {
            int index = i;
            Address address = workingList[index];

            GameObject go = Instantiate(addressBarPrefab, addressListParent);
            go.transform.Find("AddressText").GetComponent<TextMeshProUGUI>().text =
                $"{address.addressName}, {address.city}, {address.district}, {address.street}, {address.building}, {address.phoneNumber}";

            Button deleteBtn = go.transform.Find("DeleteButton").GetComponent<Button>();
            deleteBtn.onClick.RemoveAllListeners();
            deleteBtn.onClick.AddListener(() =>
            {
                workingList.RemoveAt(index);
                RebuildUIFromWorkingList();
            });
        }

        noAddressMessage.SetActive(workingList.Count == 0);
        addNewAddressButton.interactable = workingList.Count < maxAddresses;
    }

    public void AddNewAddress()
    {
        errorMessageText.text = "";
        confirmationMessage.text = "";

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

        if (workingList.Count >= maxAddresses)
        {
            errorMessageText.text = "You’ve reached the maximum number of addresses.";
            return;
        }

        Address newAddr = new(
            addressNameInput.text.Trim(),
            countryDropdown.options[countryDropdown.value].text,
            cityInput.text.Trim(),
            districtInput.text.Trim(),
            streetInput.text.Trim(),
            buildingInput.text.Trim(),
            phoneNumberInput.text.Trim()
        );

        workingList.Add(newAddr);
        ClearInputs();
        addNewAddressForm.SetActive(false);
        RebuildUIFromWorkingList();
    }

    void SaveToFirebase()
    {
        var baseRef = dbRef.Child("REVIRA/Consumers").Child(userId).Child("AddressBook");
        baseRef.RemoveValueAsync().ContinueWithOnMainThread(_ =>
        {
            for (int i = 0; i < workingList.Count; i++)
            {
                string key = "Address" + (i + 1);
                baseRef.Child(key).SetRawJsonValueAsync(JsonUtility.ToJson(workingList[i]));
            }

            confirmationMessage.text = "Changes saved successfully.";
        });
    }

    void ClearInputs()
    {
        addressNameInput.text = cityInput.text = districtInput.text =
        streetInput.text = buildingInput.text = phoneNumberInput.text = "";
    }

    public void OnBackButtonPressed()
    {
        addNewAddressForm.SetActive(false);
        ClearInputs();
        errorMessageText.text = "";
        confirmationMessage.text = "";

        addressUpdatePanel.SetActive(false);
        profileDetailsPanel.SetActive(true);
    }
}
