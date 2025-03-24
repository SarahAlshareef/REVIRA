using UnityEngine;

public static class CheckoutManager
{
    // The promotional code used by the user
    public static string UsedPromoCode { get; set; } = "";

    // The discount percentage applied from the promo code
    public static float DiscountPercentage { get; set; } = 0f;

    // The total amount after applying the discount
    public static float DiscountedTotal { get; set; } = 0f;

    // The selected delivery company name
    public static string DeliveryCompany { get; set; } = "";

    // The delivery price of the selected company
    public static float DeliveryPrice { get; set; } = 0f;

    // The delivery duration (e.g. "2 to 5 days")
    public static string DeliveryDuration { get; set; } = "";
}
