using System.Collections.Generic;
using UnityEngine;

public static class PromotionalManager
{
    public static string UsedPromoCode { get; set; } = ""; // The promotional code used by the user
    public static float DiscountPercentage { get; set; } = 0f; // The discount percentage applied from the promo code

    public static float DiscountedTotal { get; set; } = 0f; // The total amount after applying the discount


    // Dictionary to store detailed discount per product
    public static Dictionary<string, DiscountInfo> ProductDiscounts = new Dictionary<string, DiscountInfo>();
}

public class DiscountInfo
{
    public float originalPrice;
    public float discountPercentage;
    public float discountAmount;
    public float finalPrice;
}