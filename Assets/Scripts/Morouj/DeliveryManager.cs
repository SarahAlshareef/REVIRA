using UnityEngine;

public static class DeliveryManager
{
    // The selected delivery company name
    public static string DeliveryCompany { get; set; } = "";

    // The delivery price of the selected company
    public static float DeliveryPrice { get; set; } = 0f;

    // The delivery duration (e.g. "2 to 5 days")
    public static string DeliveryDuration { get; set; } = "";
}
