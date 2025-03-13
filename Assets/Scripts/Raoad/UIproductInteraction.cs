using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIproductInteraction : MonoBehaviour
{
    public string productID; // Unique product identifier
    public GameObject productPopup; // UI panel for all products
    public Button closeButton; // Close button

    private Camera mainCamera;

    // Dictionary to store predefined UI positions for each product
    private Dictionary<string, Vector3> productPopupPositions = new Dictionary<string, Vector3>();

    void Start()
    {
        mainCamera = Camera.main;

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
            closeButton.onClick.AddListener(ClosePopup);

        // Hide popup on start
        if (productPopup != null)
        {
            productPopup.SetActive(false);
        }

        // Predefined positions for each product
        productPopupPositions.Add("product_001", new Vector3(12.23f, -2.0f, -6f));
        productPopupPositions.Add("product_002", new Vector3(12.23f, -2.0f, -5f));
        productPopupPositions.Add("product_003", new Vector3(12.23f, -2.0f, -4f));
        productPopupPositions.Add("product_004", new Vector3(12.23f, -2.0f, -3f));
        productPopupPositions.Add("product_005", new Vector3(12.23f, -2.0f, -2f));
        productPopupPositions.Add("product_006", new Vector3(12.23f, -2.0f, -1f));
        productPopupPositions.Add("product_007", new Vector3(12.23f, -2.0f, 0f));
        productPopupPositions.Add("product_008", new Vector3(12.23f, -2.0f, 1f));
        productPopupPositions.Add("product_009", new Vector3(12.23f, -2.0f, 2f));
        productPopupPositions.Add("product_010", new Vector3(12.23f, -2.0f, 3f));
        productPopupPositions.Add("product_011", new Vector3(12.23f, -2.0f, 4f));
        productPopupPositions.Add("product_012", new Vector3(12.23f, -2.0f, 5f));
        productPopupPositions.Add("product_013", new Vector3(12.23f, -2.0f, 6f));
        productPopupPositions.Add("product_014", new Vector3(12.23f, -2.0f, 7f));
        productPopupPositions.Add("product_015", new Vector3(12.23f, -2.0f, 8f));
    }

    private void OnMouseDown()
    {
        if (productPopup == null)
        {
            Debug.LogError("Product popup is not assigned in the Inspector!");
            return;
        }

        if (productPopup.activeSelf)
        {
            Debug.Log("Popup is already active, hiding it...");
            productPopup.SetActive(false);
        }
        else
        {
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
    }

    public void ClosePopup()
    {
        if (productPopup != null)
        {
            productPopup.SetActive(false); // Hide the UI panel
        }
    }
}