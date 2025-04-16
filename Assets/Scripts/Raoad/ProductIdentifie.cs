using UnityEngine;

[DisallowMultipleComponent]
public class ProductIdentifie : MonoBehaviour
{
    [Header("Product Information")]
    [SerializeField] private string storeID = "storeID_123";     
    [SerializeField] private string productID;                   
    [SerializeField] private string productName;                 

   
    public string StoreID => storeID;
    public string ProductID => productID;
    public string ProductName => productName;

    void Start()
    {
        
        if (string.IsNullOrEmpty(productID))
        {
            Debug.LogWarning($"The product \"{productName}\" does not have an assigned ID. Please set a productID in the Inspector.");
        }
    }
}