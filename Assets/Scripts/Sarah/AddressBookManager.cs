using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System.Linq;

public class AddressBookManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform toggleParent;
    public GameObject addressTogglePrefab, noAddressMessage, addNewAddressButtonImage, newAddressForm;
    public Button addNewAddressButton, saveButton, nextButton, backButton;
    public TMP_InputField addressNameInput, cityInput, districtInput, streetInput, buildingInput, phoneNumberInput;
    public TMP_Dropdown countryDropdown;
    public TextMeshProUGUI outsideErrorMessageText, formErrorMessageText, CoinText;

    [Header("Panels")]
    public GameObject promotionalPanel;
    public GameObject addressPanel;
    public GameObject methodPanel;

    private DatabaseReference dbReference;
    private readonly List<Address> addressList = new();
    private readonly List<Toggle> allToggles = new();
    private const int maxAddresses = 3;

    public static Address SelectedAddress { get; private set; }

    #region Unity Methods
    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        StartCoroutine(WaitForUserIdAndLoad());

        formErrorMessageText.text = "";
        SetupButtonListeners();
        nextButton.gameObject.SetActive(false);
        CoinText.text = UserManager.Instance.AccountBalance.ToString("F2");
    }
    #endregion

    #region UI Panel Navigation
    void ShowOnlyPanel(GameObject target)
    {
        promotionalPanel?.SetActive(false);
        addressPanel?.SetActive(false);
        methodPanel?.SetActive(false);

        target?.SetActive(true);
    }

    void ShowPanelInFront(GameObject panel)
    {
        if (panel == null) return;

        Transform cam = Camera.main.transform;
        Vector3 flatForward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
        Vector3 targetPos = cam.position + flatForward * 5f;
        targetPos.y = cam.position.y + 0.8f; // Fixed height

        panel.transform.position = targetPos;
        panel.transform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);

        ShowOnlyPanel(panel);
    }

    void SetupButtonListeners()
    {
        addNewAddressButton.onClick.AddListener(() => {
            bool isActive = newAddressForm.activeSelf;
            newAddressForm.SetActive(!isActive);

            formErrorMessageText.text = isActive ? "" : formErrorMessageText.text;
            if (isActive) ClearInputs();
        });

        saveButton.onClick.AddListener(SaveNewAddress);

        nextButton.onClick.AddListener(() => {
            ShowPanelInFront(methodPanel);
            CoinText.text = UserManager.Instance.AccountBalance.ToString("F2");
        });

        backButton.onClick.AddListener(() => {
            ShowPanelInFront(promotionalPanel);
        });
    }
    #endregion

    #region Firebase Operations
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

        addressRef.GetValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsCompleted && task.Result.Exists)
            {
                foreach (var snapshot in task.Result.Children)
                {
                    var address = JsonUtility.FromJson<Address>(snapshot.GetRawJsonValue());
                    addressList.Add(address);
                    CreateToggle(address, snapshot.Key);
                }
            }

            noAddressMessage.SetActive(addressList.Count == 0);
            nextButton.gameObject.SetActive(false);

            bool isMaxed = addressList.Count >= maxAddresses;
            addNewAddressButton.interactable = !isMaxed;
            outsideErrorMessageText.text = isMaxed ? "You’ve reached the maximum number of addresses. Delete one to add a new one." : "";
            addNewAddressButtonImage?.SetActive(!isMaxed);
        });
    }

    void SaveNewAddress()
    {
        string name = addressNameInput.text.Trim();
        string city = cityInput.text.Trim();
        string district = districtInput.text.Trim();
        string street = streetInput.text.Trim();
        string building = buildingInput.text.Trim();
        string phone = phoneNumberInput.text.Trim();

        if (new[] { name, city, district, street, building, phone }.Any(string.IsNullOrEmpty))
        {
            formErrorMessageText.text = "Please fill in all fields.";
            return;
        }

        if (!phone.StartsWith("05") || phone.Length != 10 || !phone.All(char.IsDigit))
        {
            formErrorMessageText.text = "Phone number must start with '05' and be exactly 10 digits.";
            return;
        }

        Address newAddr = new(name, "Saudi Arabia", city, district, street, building, phone);
        string path = $"REVIRA/Consumers/{UserManager.Instance.UserId}/AddressBook/Address{addressList.Count + 1}";

        dbReference.Child(path).SetRawJsonValueAsync(JsonUtility.ToJson(newAddr)).ContinueWithOnMainThread(task => {
            if (task.IsCompleted)
            {
                newAddressForm.SetActive(false);
                ClearInputs();
                LoadAddresses();
            }
        });
    }

    void DeleteAddress(string key)
    {
        string userId = UserManager.Instance.UserId;
        var baseRef = dbReference.Child("REVIRA/Consumers").Child(userId).Child("AddressBook");

        baseRef.Child(key).RemoveValueAsync().ContinueWithOnMainThread(task => {
            if (task.IsCompleted)
                ShiftAddresses(baseRef);
        });
    }

    void ShiftAddresses(DatabaseReference baseRef)
    {
        baseRef.GetValueAsync().ContinueWithOnMainThread(task => {
            if (!task.IsCompleted || !task.Result.Exists)
            {
                LoadAddresses();
                return;
            }

            List<Address> remaining = new();
            foreach (var snapshot in task.Result.Children)
                remaining.Add(JsonUtility.FromJson<Address>(snapshot.GetRawJsonValue()));

            baseRef.RemoveValueAsync().ContinueWithOnMainThread(_ => {
                for (int i = 0; i < remaining.Count; i++)
                {
                    string key = $"Address{i + 1}";
                    baseRef.Child(key).SetRawJsonValueAsync(JsonUtility.ToJson(remaining[i]));
                }
                LoadAddresses();
            });
        });
    }
    #endregion

    #region UI Helpers
    void CreateToggle(Address address, string key)
    {
        GameObject toggleObject = Instantiate(addressTogglePrefab, toggleParent);
        toggleObject.transform.Find("AddressText").GetComponent<TextMeshProUGUI>().text =
            $"{address.addressName}, {address.city}, {address.district}, {address.street}, {address.building}, {address.phoneNumber}";

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        Button deleteBtn = toggleObject.transform.Find("DeleteButton").GetComponent<Button>();

        toggle.isOn = false;
        toggle.group = toggleParent.GetComponent<ToggleGroup>();
        allToggles.Add(toggle);

        toggle.onValueChanged.AddListener(isOn => SelectDeliveryAddress(isOn, toggle, address));

        deleteBtn.onClick.AddListener(() => DeleteAddress(key));
    }

    void SelectDeliveryAddress(bool isOn, Toggle toggle, Address address)
    {
        if (isOn)
        {
            allToggles.ForEach(t => { if (t != toggle) t.isOn = false; });
            SelectedAddress = address;
        }
        else if (!allToggles.Any(t => t.isOn))
        {
            SelectedAddress = null;
        }

        nextButton.gameObject.SetActive(SelectedAddress != null);
    }

    void ClearToggles()
    {
        foreach (Transform child in toggleParent) Destroy(child.gameObject);
        addressList.Clear();
        allToggles.Clear();
    }

    void ClearInputs()
    {
        addressNameInput.text = cityInput.text = districtInput.text =
            streetInput.text = buildingInput.text = phoneNumberInput.text = "";
        formErrorMessageText.text = "";
    }
    #endregion
}
