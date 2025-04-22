using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;

public class AddressBookManager1 : MonoBehaviour
{
    [Header("UI References")]
    public Transform toggleParent;
    public GameObject addressTogglePrefab, noAddressMessage, addNewAddressButtonImage, newAddressForm;
    public Button addNewAddressButton, saveButton, nextButton, backButton;
    public TMP_InputField addressNameInput, cityInput, districtInput, streetInput, buildingInput, phoneNumberInput;
    public TMP_Dropdown countryDropdown;
    public TextMeshProUGUI outsideErrorMessageText, formErrorMessageText, CoinText;

    private DatabaseReference dbReference;
    private readonly List<Address> addressList = new();
    private readonly List<Toggle> allToggles = new();
    private const int maxAddresses = 3;

    public static Address SelectedAddress { get; private set; }

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        StartCoroutine(WaitForUserIdAndLoad());

        CoinText.text = UserManager.Instance.AccountBalance.ToString("F2");
        formErrorMessageText.text = "";

        addNewAddressButton.onClick.AddListener(ToggleNewAddressForm);
        saveButton.onClick.AddListener(SaveNewAddress);
        nextButton.onClick.AddListener(() => SceneManager.LoadScene("Method"));
        backButton.onClick.AddListener(() => SceneManager.LoadScene("Promotional"));

        nextButton.gameObject.SetActive(false);
    }

    IEnumerator WaitForUserIdAndLoad()
    {
        while (string.IsNullOrEmpty(UserManager.Instance.UserId))
            yield return null;
        LoadAddresses();
    }

    void LoadAddresses()
    {
        ClearToggles();
        string userId = UserManager.Instance.UserId;
        var addressRef = dbReference.Child("REVIRA/Consumers").Child(userId).Child("AddressBook");

        addressRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                foreach (var snap in task.Result.Children)
                {
                    var address = JsonUtility.FromJson<Address>(snap.GetRawJsonValue());
                    addressList.Add(address);
                    CreateToggle(address, snap.Key);
                }
            }

            bool hasNoAddresses = addressList.Count == 0;
            noAddressMessage.SetActive(hasNoAddresses);
            nextButton.gameObject.SetActive(false);

            bool isMaxed = addressList.Count >= maxAddresses;
            addNewAddressButton.interactable = !isMaxed;
            outsideErrorMessageText.text = isMaxed ? "You’ve reached the maximum number of addresses. Delete one to add a new one." : "";
            if (addNewAddressButtonImage) addNewAddressButtonImage.SetActive(!isMaxed);
        });
    }

    void ClearToggles()
    {
        foreach (Transform child in toggleParent)
            Destroy(child.gameObject);
        addressList.Clear();
        allToggles.Clear();
    }

    void CreateToggle(Address address, string key)
    {
        GameObject toggleGO = Instantiate(addressTogglePrefab, toggleParent);
        toggleGO.transform.Find("AddressText").GetComponent<TextMeshProUGUI>().text =
            $"{address.addressName}, {address.city}, {address.district}, {address.street}, {address.building}, {address.phoneNumber}";

        Toggle toggle = toggleGO.GetComponent<Toggle>();
        Button deleteBtn = toggleGO.transform.Find("DeleteButton").GetComponent<Button>();

        toggle.isOn = false;
        toggle.group = toggleParent.GetComponent<ToggleGroup>();
        allToggles.Add(toggle);

        toggle.onValueChanged.AddListener(isOn =>
        {
            if (!isOn) return;
            allToggles.ForEach(t => { if (t != toggle) t.isOn = false; });
            SelectedAddress = address;
            nextButton.gameObject.SetActive(true);
        });

        deleteBtn.onClick.AddListener(() => DeleteAddress(key));
    }

    void ToggleNewAddressForm()
    {
        bool isActive = newAddressForm.activeSelf;
        newAddressForm.SetActive(!isActive);

        if (!isActive)
            formErrorMessageText.text = "";
        else
            ClearInputs();
    }

    void SaveNewAddress()
    {
        if (!IsAddressFormValid(out string error))
        {
            formErrorMessageText.text = error;
            return;
        }

        Address newAddr = new(
            addressNameInput.text.Trim(),
            "Saudi Arabia",
            cityInput.text.Trim(),
            districtInput.text.Trim(),
            streetInput.text.Trim(),
            buildingInput.text.Trim(),
            phoneNumberInput.text.Trim());

        string path = $"REVIRA/Consumers/{UserManager.Instance.UserId}/AddressBook/Address{addressList.Count + 1}";

        dbReference.Child(path).SetRawJsonValueAsync(JsonUtility.ToJson(newAddr)).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                newAddressForm.SetActive(false);
                ClearInputs();
                LoadAddresses();
            }
        });
    }

    bool IsAddressFormValid(out string error)
    {
        string name = addressNameInput.text.Trim();
        string city = cityInput.text.Trim();
        string district = districtInput.text.Trim();
        string street = streetInput.text.Trim();
        string building = buildingInput.text.Trim();
        string phone = phoneNumberInput.text.Trim();

        if (new[] { name, city, district, street, building, phone }.Any(string.IsNullOrEmpty))
        {
            error = "Please fill in all fields.";
            return false;
        }

        if (!phone.StartsWith("05"))
        {
            error = "Phone number must start with '05'.";
            return false;
        }

        if (phone.Length != 10 || !phone.All(char.IsDigit))
        {
            error = "Phone number must be exactly 10 digits.";
            return false;
        }

        error = "";
        return true;
    }

    void DeleteAddress(string key)
    {
        string userId = UserManager.Instance.UserId;
        var baseRef = dbReference.Child("REVIRA/Consumers").Child(userId).Child("AddressBook");

        baseRef.Child(key).RemoveValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
                ShiftAddresses(baseRef, userId);
        });
    }

    void ShiftAddresses(DatabaseReference baseRef, string userId)
    {
        baseRef.GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompleted || !task.Result.Exists)
            {
                LoadAddresses();
                return;
            }

            List<Address> remaining = new();
            foreach (var snap in task.Result.Children)
                remaining.Add(JsonUtility.FromJson<Address>(snap.GetRawJsonValue()));

            baseRef.RemoveValueAsync().ContinueWithOnMainThread(_ =>
            {
                for (int i = 0; i < remaining.Count; i++)
                {
                    string key = $"Address{i + 1}";
                    baseRef.Child(key).SetRawJsonValueAsync(JsonUtility.ToJson(remaining[i]));
                }
                LoadAddresses();
            });
        });
    }

    void ClearInputs()
    {
        addressNameInput.text = cityInput.text = districtInput.text =
            streetInput.text = buildingInput.text = phoneNumberInput.text = "";
        formErrorMessageText.text = "";
    }
}
