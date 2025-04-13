using UnityEngine;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;

public class AddressDisplayOnly : MonoBehaviour
{
    public Transform addressParent;
    public GameObject addressBarPrefab;

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
        dbRef.Child("REVIRA/Consumers/" + userId + "/Addresses").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                foreach (Transform child in addressParent)
                    Destroy(child.gameObject);

                DataSnapshot snapshot = task.Result;
                foreach (DataSnapshot child in snapshot.Children)
                {
                    Address address = JsonUtility.FromJson<Address>(child.GetRawJsonValue());
                    GameObject bar = Instantiate(addressBarPrefab, addressParent);
                    bar.transform.Find("AddressName").GetComponent<TextMeshProUGUI>().text = address.addressName;
                    bar.transform.Find("City").GetComponent<TextMeshProUGUI>().text = address.city;
                    // Add more fields if needed
                }
            }
        });
    }
}
