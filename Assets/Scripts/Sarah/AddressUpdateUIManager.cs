using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AddressUpdateUIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject profileDetailsPanel;
    public GameObject addressUpdatePanel;

    [Header("Buttons")]
    public Button updateAddressButton;
    public Button backButton;

    [Header("Address Input Fields")]
    public TMP_InputField addressNameInput, cityInput, districtInput, streetInput, buildingInput, phoneNumberInput;

    [Header("Scripts")]
    public AddressDisplayOnly addressDisplayOnlyScript;

    void Start()
    {
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
        ClearInputs();
        addressUpdatePanel.SetActive(false);
        profileDetailsPanel.SetActive(true);
        addressDisplayOnlyScript.LoadAddresses();
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
