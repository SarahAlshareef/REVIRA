using UnityEngine;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections;

public class AddressDisplayOnly : MonoBehaviour
{
    public Transform addressParent;
    public GameObject addressBarPrefab;

    private DatabaseReference dbRef;
    private string userId;

    void OnEnable()
    {
        StartCoroutine(WaitForUserIdAndLoad());
    }

    IEnumerator WaitForUserIdAndLoad()
    {
        while (string.IsNullOrEmpty(UserManager.Instance.UserId))
            yield return null;

        userId = UserManager.Instance.UserId;
        LoadAddresses();
    }

    public void LoadAddresses()
    {
        if (string.IsNullOrEmpty(userId))
            userId = UserManager.Instance.UserId;

        dbRef = FirebaseDatabase.DefaultInstance.RootReference;

        foreach (Transform child in addressParent)
            Destroy(child.gameObject);

        dbRef.Child("REVIRA/Consumers/" + userId + "/AddressBook").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                foreach (DataSnapshot snap in task.Result.Children)
                {
                    Address address = JsonUtility.FromJson<Address>(snap.GetRawJsonValue());

                    GameObject bar = Instantiate(addressBarPrefab, addressParent);
                    var textComponent = bar.transform.Find("AddressText")?.GetComponent<TextMeshProUGUI>();

                    if (textComponent != null)
                    {
                        textComponent.text = $"{address.addressName}, {address.city}, {address.district}, {address.street}, {address.building}, {address.phoneNumber}";
                    }
                }
            }
        });
    }
}
