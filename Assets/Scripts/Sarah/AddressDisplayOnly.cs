using UnityEngine;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;

public class AddressDisplayOnly : MonoBehaviour
{
    public Transform addressParent;
    public GameObject addressBarPrefab; // Prefab that includes "AddressText" TMP

    private DatabaseReference dbRef;
    private string userId;

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;
        LoadAddresses();
    }

    void LoadAddresses()
    {
        dbRef.Child("REVIRA/Consumers/" + userId + "/AddressBook").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                foreach (Transform child in addressParent)
                    Destroy(child.gameObject);

                foreach (DataSnapshot snap in task.Result.Children)
                {
                    Address address = JsonUtility.FromJson<Address>(snap.GetRawJsonValue());

                    GameObject bar = Instantiate(addressBarPrefab, addressParent);

                    string fullAddress = $"{address.addressName}, {address.city}, {address.district}, {address.street}, {address.building}, {address.phoneNumber}";

                    bar.transform.Find("AddressText").GetComponent<TextMeshProUGUI>().text = fullAddress;
                }
            }
        });
    }
}
