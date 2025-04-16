using UnityEngine;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;

public class ProductDetailsPanel : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_Text nameText;
    public TMP_Text priceText;
    public TMP_Text descriptionText;

    public void LoadProduct(string productID)
    {
        string storeID = "storeID_123"; 
        string path = $"REVIRA/stores/{storeID}/products/{productID}";

        FirebaseDatabase.DefaultInstance
            .GetReference(path)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    var data = task.Result;
                    nameText.text = data.Child("name").Value.ToString();
                    priceText.text = data.Child("price").Value.ToString() + " SAR";
                    descriptionText.text = data.Child("description").Value.ToString();
                }
                else
                {
                    Debug.LogWarning($"Product not found in Firebase: {path}");
                    nameText.text = "Not Found";
                    priceText.text = "";
                    descriptionText.text = "";
                }
            });
    }
}