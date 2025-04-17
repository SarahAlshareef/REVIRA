using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;

public class OrderDetailsManager : MonoBehaviour
{
    [Header("Product Display")]
    public GameObject productItemPrefab; 
    public Transform productListParent;  

    [Header("Text Fields")]
    public TextMeshProUGUI totalText;
    public TextMeshProUGUI orderIdText;
    public TextMeshProUGUI orderDateText;
    public TextMeshProUGUI promoCodeText;
    public TextMeshProUGUI deliveryText;
    public TextMeshProUGUI addressText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI deliveryCompanyText;

    private DatabaseReference dbRef;
    private string userId;

    void Awake()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;
    }

    public void DisplayOrderDetails(string orderId)
    {
        dbRef.Child("REVIRA/Consumers").Child(userId).Child("OrderHistory").Child(orderId)
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted && task.Result.Exists)
                {
                    var data = task.Result;

                    foreach (Transform child in productListParent)
                        Destroy(child.gameObject);

                    foreach (var product in data.Child("items").Children)
                    {
                        string name = product.Child("productName").Value?.ToString();
                        string size = "";
                        string quantity = "0";

                        foreach (var s in product.Child("sizes").Children)
                        {
                            size = s.Key;
                            quantity = s.Value.ToString();
                        }

                        string price = product.Child("price").Value?.ToString();
                        string discount = product.Child("discountAmount")?.Value?.ToString() ?? "0";

                        GameObject productGO = Instantiate(productItemPrefab, productListParent);
                        productGO.transform.Find("Text (Product name)").GetComponent<TextMeshProUGUI>().text = name;
                        productGO.transform.Find("Text (Size)").GetComponent<TextMeshProUGUI>().text = size;
                        productGO.transform.Find("Text (Quantity)").GetComponent<TextMeshProUGUI>().text = quantity;
                        productGO.transform.Find("Text (Price)").GetComponent<TextMeshProUGUI>().text = price;
                        productGO.transform.Find("Text (Discount)").GetComponent<TextMeshProUGUI>().text = "-" + discount;
                    }

                    
                    orderIdText.text = data.Child("orderId").Value?.ToString();
                    orderDateText.text = data.Child("orderDate").Value?.ToString();
                    promoCodeText.text = data.Child("usedPromoCode").Value?.ToString();
                    deliveryText.text = data.Child("deliveryPrice").Value?.ToString();
                    statusText.text = data.Child("orderStatus").Value?.ToString();
                    deliveryCompanyText.text = data.Child("deliveryCompany").Value?.ToString();
                    totalText.text = data.Child("finalPrice").Value?.ToString();

                    
                    string addr = $"{data.Child("addressName").Value}, {data.Child("country").Value}, {data.Child("city").Value}, {data.Child("district").Value}, {data.Child("street").Value}, {data.Child("building").Value}, {data.Child("phoneNumber").Value}";
                    addressText.text = addr;
                }
                else
                {
                    Debug.LogWarning("Order not found: " + orderId);
                }
            });
    }
    public void GoBack()
    {
        gameObject.SetActive(false); 
        ViewOrderManager.Instance.orderListPanel.SetActive(true); 
    }
}

