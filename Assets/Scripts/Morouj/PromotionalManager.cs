using UnityEngine;

public static class PromotionalManager
{
    // The promotional code used by the user
    public static string UsedPromoCode { get; set; } = "";

    // The discount percentage applied from the promo code
    public static float DiscountPercentage { get; set; } = 0f;

    // The total amount after applying the discount
    public static float DiscountedTotal { get; set; } = 0f;
}
