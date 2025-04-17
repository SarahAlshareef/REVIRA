using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ViewOrderManager : MonoBehaviour
{
    public static ViewOrderManager Instance;

    [Header("UI Elements")]
    public GameObject orderItemPrefab;
    public Transform orderListParent;
    public GameObject orderListPanel;
    public GameObject orderDetailsPanel;
    public OrderDetailsManager detailsManager;

    private string userId;
    private DatabaseReference dbRef;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        StartCoroutine(WaitForUserIdAndLoadOrders());
    }

    IEnumerator WaitForUserIdAndLoadOrders()
    {
        while (string.IsNullOrEmpty(UserManager.Instance.UserId))
            yield return null;

        userId = UserManager.Instance.UserId;
        dbRef = FirebaseDatabase.DefaultInstance.RootReference;
        LoadOrders();
    }

    public void LoadOrders()
    {
        foreach (Transform child in orderListParent)
            Destroy(child.gameObject);

        dbRef.Child("REVIRA").Child("Consumers").Child(userId).Child("OrderHistory")
            .GetValueAsync().ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompleted || task.Result == null || !task.Result.Exists)
                {
                    Debug.Log("No orders found.");
                    return;
                }

                foreach (var orderSnapshot in task.Result.Children)
                {
                    string orderId = orderSnapshot.Child("orderId").Value?.ToString();
                    string orderDate = orderSnapshot.Child("orderDate").Value?.ToString();
                    string status = orderSnapshot.Child("orderStatus").Value?.ToString();
                    string finalPrice = orderSnapshot.Child("finalPrice").Value?.ToString();

                    GameObject item = Instantiate(orderItemPrefab, orderListParent);

                    item.transform.Find("Text (Id)").GetComponent<TextMeshProUGUI>().text = "#" + orderId;
                    item.transform.Find("Text (Date)").GetComponent<TextMeshProUGUI>().text = orderDate;
                    item.transform.Find("Text (Status)").GetComponent<TextMeshProUGUI>().text = status;
                    item.transform.Find("Text (Total)").GetComponent<TextMeshProUGUI>().text = finalPrice;

                    Button viewButton = item.transform.Find("Button (view Details)").GetComponent<Button>();
                    viewButton.onClick.AddListener(() =>
                    {
                        orderListPanel.SetActive(false);
                        orderDetailsPanel.SetActive(true);
                        detailsManager.DisplayOrderDetails(orderSnapshot);
                    });
                }
            });
    }
}


