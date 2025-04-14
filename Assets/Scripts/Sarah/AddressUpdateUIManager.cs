using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AddressUpdateUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject profileDetailsPanel;
    public GameObject addressUpdatePanel;

    [Header("Buttons")]
    public Button updateAddressButton;  // From Profile Details
    public Button backButton;           // From Address Update

    [Header("Address Input Fields")]
    public TMP_InputField addressNameInput, cityInput, districtInput, streetInput, buildingInput, phoneNumberInput;

    void Start()
    {
        // Start with Profile Details visible
        profileDetailsPanel.SetActive(true);
        addressUpdatePanel.SetActive(false);

        updateAddressButton.onClick.AddListener(SwitchToAddressUpdate);
        backButton.onClick.AddListener(BackToProfileDetails);
    }

    void SwitchToAddressUpdate()
    {
        profileDetailsPanel.SetActive(false);
        addressUpdatePanel.SetActive(true);
    }

    void BackToProfileDetails()
    {
        ClearInputs(); // Don't save anything
        profileDetailsPanel.SetActive(true);
        addressUpdatePanel.SetActive(false);
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
