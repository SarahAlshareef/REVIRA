using UnityEngine;
using TMPro;

public class RecallAddress : MonoBehaviour
{
    public TextMeshProUGUI addressText;

    void Start()
    {
        var address = AddressBookManager.SelectedAddress;

        if (address != null)
        {
            addressText.text = $"{address.addressName}, {address.city}, {address.district}, {address.street}, {address.building}, {address.phoneNumber}";
        }
        else
        {
            addressText.text = "No address selected.";
        }
    }
}
