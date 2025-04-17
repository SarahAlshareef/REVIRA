using UnityEngine;
using UnityEngine.UI;

public class OrderNavigation : MonoBehaviour
{
    [Header("Panels")]
    public GameObject viewOrderPanel;     
    public GameObject viewDetailsPanel;   

    [Header("Buttons")]
    public Button sidebarViewOrderButton; 
    public Button backButton;             

    void Start()
    {
        sidebarViewOrderButton?.onClick.AddListener(ShowViewOrders);
        backButton?.onClick.AddListener(BackToOrders);
    }

    public void ShowViewOrders()
    {
        viewOrderPanel.SetActive(true);
        viewDetailsPanel.SetActive(false);

        if (ViewOrderManager.Instance != null)
            ViewOrderManager.Instance.LoadOrders();
    }

    public void BackToOrders()
    {
        viewDetailsPanel.SetActive(false);
        viewOrderPanel.SetActive(true);

        if (ViewOrderManager.Instance != null)
            ViewOrderManager.Instance.LoadOrders();
    }
}


