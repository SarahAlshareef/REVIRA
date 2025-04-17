using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;

public class ViewOrderManager : MonoBehaviour
{
    public static ViewOrderManager Instance;

    [Header("Order List UI")]
    public GameObject orderItemPrefab;     
    public Transform contentParent;        

    [Header("Panels")]
    public GameObject orderListPanel;      
    public GameObject orderDetailsPanel;   

    [Header("Details Manager")]
    public OrderDetailsManager detailsManager;

    private DatabaseReference dbRef;
    private string userId;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        userId = UserManager.Instance.UserId;

        LoadOrders();
    }

    void LoadOrders()
    {
        dbRef.Child("REVIRA/Consumers").Child(userId).Child("OrderHistory").GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted && task.Result.Exists)
            {
                // Õ–› «·⁄‰«’— «·ﬁœÌ„…
                foreach (Transform child in contentParent)
                    Destroy(child.gameObject);

                //  ﬂ—«— «·ÿ·»« 
                foreach (var order in task.Result.Children)
                {
                    string orderId = order.Key;
                    string orderDate = order.Child("orderDate").Value?.ToString() ?? "N/A";
                    string status = order.Child("orderStatus").Value?.ToString() ?? "Pending";
                    string total = order.Child("finalPrice").Value?.ToString() ?? "0.00";

                    GameObject orderGO = Instantiate(orderItemPrefab, contentParent);

                    orderGO.transform.Find("Text (Id)").GetComponent<TextMeshProUGUI>().text = orderId;
                    orderGO.transform.Find("Text (Date)").GetComponent<TextMeshProUGUI>().text = orderDate;
                    orderGO.transform.Find("Text (Status)").GetComponent<TextMeshProUGUI>().text = status;
                    orderGO.transform.Find("Text (Total)").GetComponent<TextMeshProUGUI>().text = total;

                    // »—„Ã… “— View Details
                    Button viewDetailsBtn = orderGO.transform.Find("Button (view Details)").GetComponent<Button>();
                    viewDetailsBtn.onClick.AddListener(() =>
                    {
                        ShowOrderDetails(orderId);
                    });
                }
            }
            else
            {
                Debug.LogWarning("No orders found for this user.");
            }
        });
    }

    public void ShowOrderDetails(string orderId)
    {
        orderListPanel.SetActive(false);
        orderDetailsPanel.SetActive(true);

        if (detailsManager != null)
        {
            detailsManager.DisplayOrderDetails(orderId);
        }
    }
}


