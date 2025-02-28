

using UnityEngine;
using UnityEngine.UI; // For UI interactions (Buttons, Images, etc.)
using System.Collections;
using System.Collections.Generic;
using TMPro; // For text updates
using UnityEngine.SceneManagement; // If scene transitions are required
using UnityEngine;
using UnityEngine.UI;

public class ProductPopup : MonoBehaviour
{
    public GameObject mainPanel;  // Main interface panel
    public GameObject productSpecPanel; // Product specification panel
    public Button previewSpecButton; // Button to open the specification panel
    public Button closeSpecButton; // Button to close the specification panel

    void Start()
    {
        // Ensure the main panel is active and the specification panel is hidden on start
        mainPanel.SetActive(true);
        productSpecPanel.SetActive(false);

        // Bind buttons to their respective functions
        if (previewSpecButton != null)
            previewSpecButton.onClick.AddListener(ShowProductSpecification);
        else
            Debug.LogError("Preview Specification Button is not assigned!");

        if (closeSpecButton != null)
            closeSpecButton.onClick.AddListener(HideProductSpecification);
        else
            Debug.LogError("Close Specification Button is not assigned!");
    }

    void ShowProductSpecification()
    {
        mainPanel.SetActive(false);  // Hide the main interface
        productSpecPanel.SetActive(true); // Show the product specification panel
    }

    void HideProductSpecification()
    {
        productSpecPanel.SetActive(false); // Hide the product specification panel
        mainPanel.SetActive(true);  // Show the main interface
    }
}


