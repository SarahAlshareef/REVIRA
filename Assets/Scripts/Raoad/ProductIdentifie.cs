using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductIdentifie : MonoBehaviour

{
    [Header("Product Information")]
    [SerializeField] private string storeID = "storeID_123"; // Unique Store ID
    [SerializeField] private string productID; // Unique Product ID
    [SerializeField] private string productName; // Product Name

    // Getters and Setters (optional)
    public string GetStoreID() => storeID;
    public string GetProductID() => productID;
    public string GetProductName() => productName;

    void Start()
    {
        // Check if the product ID is assigned, to avoid missing configurations
        if (string.IsNullOrEmpty(productID))
        {
            Debug.LogWarning("The product {productName} does not have an assigned ID! Please set an ID in the Inspector.");
        }

    }
}