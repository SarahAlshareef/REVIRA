using UnityEngine;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;

public class OrderDetailsManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject productItemPrefab;
    public Transform productListParent;

    public TextMeshProUGUI totalText;
    public TextMeshProUGUI orderIdText;
    public TextMeshProUGUI orderDateText;
    public TextMeshProUGUI promoCodeText;
    public TextMeshProUGUI deliveryText;
    public TextMeshProUGUI addressText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI deliveryCompanyText;

    [Header("Panels")]
    public GameObject orderListPanel;
    public GameObject orderDetailsPanel;

    public static OrderDetailsManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public void DisplayOrderDetails(DataSnapshot data)
    {
        foreach (Transform child in productListParent)
            Destroy(child.gameObject);

        orderIdText.text = "#" + data.Child("orderId").Value?.ToString();
        orderDateText.text = data.Child("orderDate").Value?.ToString();
        statusText.text = data.Child("orderStatus").Value?.ToString();
        promoCodeText.text = data.Child("usedPromoCode").Value?.ToString() ?? "-";
        deliveryText.text = data.Child("deliveryPrice").Value?.ToString();
        deliveryCompanyText.text = data.Child("deliveryCompany").Value?.ToString();
        totalText.text = data.Child("finalPrice").Value?.ToString();

        
        string fullAddress = $"{data.Child("addressName").Value}, {data.Child("city").Value}, {data.Child("district").Value}, {data.Child("street").Value}, {data.Child("building").Value}, {data.Child("phoneNumber").Value}";
        addressText.text = fullAddress;

        
        if (data.HasChild("items"))
        {
            foreach (var item in data.Child("items").Children)
            {
                string name = item.Child("productName").Value?.ToString();
                string basePrice = item.Child("priceAfterDiscount").Value?.ToString() ?? "-";
                string promoPrice = item.Child("priceAfterPromoDiscount").Value?.ToString() ?? basePrice;

                float.TryParse(basePrice, out float baseP);
                float.TryParse(promoPrice, out float promoP);
                float discountAmount = baseP - promoP;
                string discountDisplay = discountAmount > 0 ? "-" + discountAmount.ToString("0.##") : "-";


                string size = "";
                string quantity = "";

                if (item.Child("sizes").HasChildren)
                {
                    foreach (var sizeEntry in item.Child("sizes").Children)
                    {
                        size = sizeEntry.Key;
                        quantity = sizeEntry.Value.ToString();
                        break; 
                    }
                }

                GameObject productGO = Instantiate(productItemPrefab, productListParent);
                productGO.transform.Find("Text (Product name)").GetComponent<TextMeshProUGUI>().text = name;
                productGO.transform.Find("Text (Size)").GetComponent<TextMeshProUGUI>().text = size;
                productGO.transform.Find("Text (Quantity)").GetComponent<TextMeshProUGUI>().text = quantity;
                productGO.transform.Find("Text (Price)").GetComponent<TextMeshProUGUI>().text = basePrice;
                productGO.transform.Find("Text (Discount").GetComponent<TextMeshProUGUI>().text = discountDisplay;
                
            }
        }
    }
}




