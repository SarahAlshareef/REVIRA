using UnityEngine;

public static class CheckoutManager
{
    public static string UsedPromoCode { get; set; } = "";
    public static float DiscountPercentage { get; set; } = 0f;
    public static float DiscountedTotal { get; set; } = 0f;
}
