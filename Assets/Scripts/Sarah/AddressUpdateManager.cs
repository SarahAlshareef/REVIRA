using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    [Header("Scripts")]
    public ShowInformation ShowInfoScript;

    [Header("Visuals")]
    public GameObject addNewAddressButtonImage;

    private DatabaseReference dbRef;
    private string userId;
    private List<Address> workingList = new();
    private const int maxAddresses = 3;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        StartCoroutine(WaitUntilUserReady());

        errorMessageText.text = "";
        confirmationMessage.text = "";
        errorMessageText.gameObject.SetActive(false);
        confirmationMessage.gameObject.SetActive(false);

        addNewAddressButton.onClick.AddListener(() =>
        {
            bool isActive = addNewAddressForm.activeSelf;
            addNewAddressForm.SetActive(!isActive);
            if (isActive) ClearInputs();
            HideMessages();
        });

        confirmAddButton.onClick.AddListener(AddNewAddress);

        discardButton.onClick.AddListener(() =>
        {
            ClearInputs();
            HideMessages();
            LoadFromFirebase();
        });

        saveButton.onClick.AddListener(SaveToFirebase);
        backButton.onClick.AddListener(OnBackButtonPressed);
    }

    IEnumerator WaitUntilUserReady()
    {
        while (string.IsNullOrEmpty(UserManager.Instance.UserId))
            yield return null;

        userId = UserManager.Instance.UserId;
        LoadFromFirebase();
    }

    public void OnOpenAddressUpdatePanel()
    {
        // Ensure the form is hidden even if enabled in editor
        if (addNewAddressForm.activeSelf)
            addNewAddressForm.SetActive(false);

        ClearInputs();
        HideMessages();

        workingList.Clear();
        foreach (Transform child in addressListParent)
            Destroy(child.gameObject);

        LoadFromFirebase();
    }

    void LoadFromFirebase()
    {
        workingList.Clear();

        foreach (Transform child in addressListParent)
            Destroy(child.gameObject);

        dbRef.Child("REVIRA/Consumers/" + userId + "/AddressBook").GetValueAsync().ContinueWithOnMainThread(task =>
        {
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

        bool canAddMore = workingList.Count < maxAddresses;
        addNewAddressButton.interactable = canAddMore;

        if (addNewAddressButtonImage != null)
            addNewAddressButtonImage.SetActive(canAddMore);
    }

    public void AddNewAddress()
    {
        HideMessages();

        if (string.IsNullOrEmpty(addressNameInput.text) || string.IsNullOrEmpty(cityInput.text) ||
            string.IsNullOrEmpty(districtInput.text) || string.IsNullOrEmpty(streetInput.text) ||
            string.IsNullOrEmpty(buildingInput.text) || string.IsNullOrEmpty(phoneNumberInput.text))
        {
            errorMessageText.text = "Please fill in all fields.";
            errorMessageText.gameObject.SetActive(true);
            return;
        }

        if (!phoneNumberInput.text.StartsWith("05") || phoneNumberInput.text.Length != 10 || !phoneNumberInput.text.All(char.IsDigit))
        {
            errorMessageText.text = "Phone number must start with '05' and be 10 digits.";
            errorMessageText.gameObject.SetActive(true);
            return;
        }

        if (workingList.Count >= maxAddresses)
        {
            errorMessageText.text = "You’ve reached the maximum number of addresses.";
            errorMessageText.gameObject.SetActive(true);
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
        HideMessages();

        var baseRef = dbRef.Child("REVIRA/Consumers").Child(userId).Child("AddressBook");
        baseRef.RemoveValueAsync().ContinueWithOnMainThread(_ =>
        {
            List<Task> saveTasks = new();

            for (int i = 0; i < workingList.Count; i++)
            {
                string key = "Address" + (i + 1);
                var task = baseRef.Child(key).SetRawJsonValueAsync(JsonUtility.ToJson(workingList[i]));
                saveTasks.Add(task);
            }

            Task.WhenAll(saveTasks).ContinueWithOnMainThread(__ =>
            {
                confirmationMessage.text = "Changes saved successfully.";
                confirmationMessage.gameObject.SetActive(true);

                if (ShowInfoScript != null)
                    ShowInfoScript.addressScript.LoadAddresses();
            });
        });
    }

    void ClearInputs()
    {
        addressNameInput.text = cityInput.text = districtInput.text =
        streetInput.text = buildingInput.text = phoneNumberInput.text = "";
    }

    void HideMessages()
    {
        errorMessageText.text = "";
        confirmationMessage.text = "";
        errorMessageText.gameObject.SetActive(false);
        confirmationMessage.gameObject.SetActive(false);
    }

    public void OnBackButtonPressed()
    {
        addNewAddressForm.SetActive(false);
        ClearInputs();
        HideMessages();

        addressUpdatePanel.SetActive(false);
        profileDetailsPanel.SetActive(true);

        if (ShowInfoScript != null)
            ShowInfoScript.addressScript.LoadAddresses();
    }
}
