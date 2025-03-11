using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIproductPopup : MonoBehaviour
{
  
    public GameObject mainPanel;  // The first UI panel (Preview Product & Specification)
    public GameObject productSpecPanel; // The product specification panel

    public Button previewSpecButton; // Button to open the specification panel
    public Button closeSpecButton; // Button to close the specification panel
    public Button closeMainButton; // Button to close the entire UI panel

    void Start()
    {
        // Ensure the main panel is visible and the specification panel is hidden at startup
        mainPanel.SetActive(true);
        productSpecPanel.SetActive(false);

        // Assign button functions
        if (previewSpecButton != null)
            previewSpecButton.onClick.AddListener(ShowProductSpecification);
        else
            Debug.LogError("Preview Specification Button is not assigned!");

        if (closeSpecButton != null)
            closeSpecButton.onClick.AddListener(HideProductSpecification);
        else
            Debug.LogError("Close Specification Button is not assigned!");

        if (closeMainButton != null)
            closeMainButton.onClick.AddListener(CloseProductPopup);
        else
            Debug.LogError("Close Main Panel Button is not assigned!");
    }

    void ShowProductSpecification()
    {
        mainPanel.SetActive(false);  // Hide the main panel
        productSpecPanel.SetActive(true); // Show the specification panel
    }

    void HideProductSpecification()
    {
        productSpecPanel.SetActive(false); // Hide the specification panel
        mainPanel.SetActive(true);  // Show the main panel again
    }

    void CloseProductPopup()
    {
        gameObject.SetActive(false); // Hide the product UI panel completely
    }
}
