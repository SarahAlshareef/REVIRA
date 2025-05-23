
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIproductInteraction : MonoBehaviour
{
    public string productID; // Unique product identifier
    public GameObject productPopup; // UI panel for all products
    public Button closeButton; // Close button

    // Dictionary to store predefined UI positions for each product
    private static Dictionary<string, Vector3> productPopupPositions = new Dictionary<string, Vector3>();

    void Start()
    {
      
        // Ensure productPopup is assigned
        if (productPopup == null)
        {
            productPopup = GameObject.Find("pop up (preview) (effect) (1)");
            if (productPopup == null)
                Debug.LogError(gameObject.name + " productPopup is NULL!");
        }

        // Ensure closeButton is assigned
        if (closeButton == null)
        {
            closeButton = GameObject.Find("Button (Cancel) (5)").GetComponent<Button>();
            if (closeButton == null)
                Debug.LogError(gameObject.name + " closeButton is NULL!");
        }

        // Assign close button functionality
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePopup);
            Debug.Log("Close button assigned successfully.");
        }

        // Hide popup on start
        if (productPopup != null)
        {
            productPopup.SetActive(false);
        }

        // Predefined positions for each product (Ensures it's assigned only once)
        if (productPopupPositions.Count == 0)
        {
            productPopupPositions.Add("product_001", new Vector3(12.23f, -2.0f, -5.8f));
            productPopupPositions.Add("product_002", new Vector3(12.23f, -2.0f, -4.8f));
            productPopupPositions.Add("product_003", new Vector3(12.23f, -2.0f, -3.8f));
            productPopupPositions.Add("product_004", new Vector3(12.23f, -2.0f, -2.8f));
            productPopupPositions.Add("product_005", new Vector3(12.23f, -2.0f, -1.8f));
            productPopupPositions.Add("product_006", new Vector3(12.23f, -2.0f, -0.8f));
            productPopupPositions.Add("product_007", new Vector3(12.23f, -2.0f, 0.2f));
            productPopupPositions.Add("product_008", new Vector3(12.23f, -2.0f, 1.2f));
            productPopupPositions.Add("product_009", new Vector3(12.23f, -2.0f, 2.2f));
            productPopupPositions.Add("product_010", new Vector3(12.23f, -2.0f, 3.2f));
            productPopupPositions.Add("product_011", new Vector3(12.23f, -2.0f, 4.2f));
            productPopupPositions.Add("product_012", new Vector3(12.23f, -2.0f, 5.2f));
            productPopupPositions.Add("product_013", new Vector3(12.23f, -2.0f, 6.2f));
            productPopupPositions.Add("product_014", new Vector3(12.23f, -2.0f, 7.2f));
            productPopupPositions.Add("product_015", new Vector3(12.23f, -2.0f, 8.2f));
        }
    }

    private void OnMouseDown()
    {
        if (productPopup == null)
        {
            Debug.LogError("Product popup is not assigned in the Inspector!");
            return;
        }

        // Close all other open popups before opening a new one
        UIproductInteraction[] allPopups = FindObjectsOfType<UIproductInteraction>();
        foreach (var popup in allPopups)
        {
            if (popup.productPopup != null && popup.productPopup.activeSelf)
            {
                popup.productPopup.SetActive(false);
            }
        }

        // Open the popup for the current product
        if (productPopupPositions.ContainsKey(productID))
        {
            Vector3 popupPosition = productPopupPositions[productID];

            // Move the popup to the predefined position
            productPopup.transform.position = popupPosition;

            Debug.Log("Showing popup for: " + productID + " at position: " + popupPosition);
            productPopup.SetActive(true);
        }
        else
        {
            Debug.LogError("No predefined position found for product: " + productID);
        }
    }

    public void ClosePopup()
    {
        if (productPopup != null)
        {
            productPopup.SetActive(false); // Hide the popup when clicking the close button
        }
    }
}
