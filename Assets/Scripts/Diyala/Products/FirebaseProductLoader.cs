using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Database; 
using Firebase.Extensions;

public class FirebaseProductLoader : MonoBehaviour
{
    private DatabaseReference dbReference; 

    void Start()
    {
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;

        LoadProductData("storeID_123", "Product_001");
    }

    void LoadProductData(string storeID, string productID)
    {
        dbReference.Child("stores").Child(storeID).Child("products").Child(productID)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted) 
                {
                    DataSnapshot snapshot = task.Result; 
                    if (snapshot.Exists) 
                    {
                        string jsonData = snapshot.GetRawJsonValue(); 
                        Debug.Log("Product Data: " + jsonData); 
                    }
                    else
                    {
                        Debug.LogWarning("Product not found in Firebase!"); 
                    }
                }
                else
                {
                    Debug.LogError("Error loading data: " + task.Exception); 
                }
            });
    }
}