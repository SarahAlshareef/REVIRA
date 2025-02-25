using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // For text updates
using UnityEngine.SceneManagement; // If scene transitions are required
using UnityEngine.UI; // For UI interactions (Buttons, Images, etc.)


public class ProductPopup : MonoBehaviour
{
    public GameObject popupWindow; // The main pop-up window
    public GameObject rotationView; // The product rotation view
    public GameObject specificationView; // The specification window
    public Button previewProductButton; // Button for product rotation
    public Button reviewProductSpecificationButton; // Button for product specification
    public Button closeButton;
    public TMP_Text productName; // Text component for product name (if needed)

    void Start()
    {
        // Ensure the pop-up is hidden initially
        popupWindow.SetActive(false);
        rotationView.SetActive(false);
        specificationView.SetActive(false);

        // Assign button listeners
        previewProductButton.onClick.AddListener(OpenRotationView);
        reviewProductSpecificationButton.onClick.AddListener(OpenSpecificationView);
        closeButton.onClick.AddListener(ClosePopup);
    }

    // Show pop-up when product is clicked
    public void ShowPopup(string name)
    {
        popupWindow.SetActive(true);
        if (productName != null)
        {
            productName.text = name; // Update product name dynamically
        }
    }

    // Open rotation view
    void OpenRotationView()
    {
        rotationView.SetActive(true);
        popupWindow.SetActive(false); // Hide main popup
    }

    // Open product specification view
    void OpenSpecificationView()
    {
        specificationView.SetActive(true);
        popupWindow.SetActive(false); // Hide main popup
    }

    // Close the pop-up
    void ClosePopup()
    {
        popupWindow.SetActive(false);
    }
}
