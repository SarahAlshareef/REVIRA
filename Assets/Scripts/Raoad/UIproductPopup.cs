using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIProductPopup : MonoBehaviour
{
    public TMP_Dropdown colorDropdown;
    public TMP_Dropdown sizeDropdown;
    public TMP_Dropdown quantityDropdown;
    public Button addToCartButton;
    public TextMeshProUGUI errorText;

    private void Start()
    {
       
        colorDropdown.onValueChanged.AddListener(delegate { UpdateSizeDropdown(); });
        sizeDropdown.onValueChanged.AddListener(delegate { UpdateQuantityDropdown(); });
        quantityDropdown.onValueChanged.AddListener(delegate { ValidateSelection(); });

        
        addToCartButton.interactable = false;
        addToCartButton.onClick.AddListener(AddToCart);
    }

   
    public void UpdateSizeDropdown()
    {
        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        if (selectedColor == "Select Color")
        {
            ShowError("Please select a color first!");
            sizeDropdown.interactable = false;
            return;
        }

        Debug.Log("Selected Color: " + selectedColor);
        sizeDropdown.interactable = true;
        errorText.text = "";
    }

   
    public void UpdateQuantityDropdown()
    {
        string selectedSize = sizeDropdown.options[sizeDropdown.value].text;
        if (selectedSize == "Select Size")
        {
            ShowError("Please select a size first!");
            quantityDropdown.interactable = false;
            return;
        }

        Debug.Log("Selected Size: " + selectedSize);
        quantityDropdown.interactable = true;
        errorText.text = "";
    }

    
    public void ValidateSelection()
    {
        string selectedColor = colorDropdown.options[colorDropdown.value].text;
        string selectedSize = sizeDropdown.options[sizeDropdown.value].text;
        string selectedQuantity = quantityDropdown.options[quantityDropdown.value].text;

        if (selectedColor == "Select Color")
        {
            ShowError("Please select a color first!");
            return;
        }
        if (selectedSize == "Select Size")
        {
            ShowError("Please select a size first!");
            return;
        }
        if (selectedQuantity == "Select Quantity")
        {
            ShowError("Please select a quantity!");
            return;
        }

        Debug.Log("Selected Color: " + selectedColor);
        Debug.Log("Selected Size: " + selectedSize);
        Debug.Log("Selected Quantity: " + selectedQuantity);

        errorText.text = "";
        addToCartButton.interactable = true;
    }

   
    public void AddToCart()
    {
        Debug.Log(" Product added to cart successfully!");
    }

    
    private void ShowError(string message)
    {
        errorText.text = message;
        errorText.color = Color.red;
        addToCartButton.interactable = false;
    }
}
