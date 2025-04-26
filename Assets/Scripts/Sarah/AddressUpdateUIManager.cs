using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AddressUpdateUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject profileDetailsPanel;
    public GameObject addressUpdatePanel;

    [Header("Buttons")]
    public Button updateAddressButton;  // Button from Profile Details
    public Button backButton;           // Button inside Address Update panel

    [Header("Address Input Fields")]
    public TMP_InputField addressNameInput, cityInput, districtInput, streetInput, buildingInput, phoneNumberInput;

    [Header("Managers")]
    public AddressUpdateManager addressUpdateManager; // Assign this in the Inspector

    [Header("Navigation Buttons")]
    public Button navButton1;
    public Button navButton2;
    public Button navButton3;

    void Start()
    {
        // Initial state
        addressUpdatePanel.SetActive(false);

        updateAddressButton.onClick.AddListener(SwitchToAddressUpdate);
        backButton.onClick.AddListener(BackToProfileDetails);
    }

    void SwitchToAddressUpdate()
    {
        profileDetailsPanel.SetActive(false);
        addressUpdatePanel.SetActive(true);

        if (addressUpdateManager != null)
            addressUpdateManager.OnOpenAddressUpdatePanel();

        // Disable navigation buttons
        if (navButton1 != null) navButton1.interactable = false;
        if (navButton2 != null) navButton2.interactable = false;
        if (navButton3 != null) navButton3.interactable = false;
    }

    void BackToProfileDetails()
    {
        ClearInputs();
        profileDetailsPanel.SetActive(true);
        addressUpdatePanel.SetActive(false);

        // Enable navigation buttons
        if (navButton1 != null) navButton1.interactable = true;
        if (navButton2 != null) navButton2.interactable = true;
        if (navButton3 != null) navButton3.interactable = true;
    }

    void ClearInputs()
    {
        addressNameInput.text = "";
        cityInput.text = "";
        districtInput.text = "";
        streetInput.text = "";
        buildingInput.text = "";
        phoneNumberInput.text = "";
    }
}
