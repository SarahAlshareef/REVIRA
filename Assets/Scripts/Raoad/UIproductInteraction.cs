using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIproductInteraction : MonoBehaviour

{
    public string productID; // The product identifier
    public GameObject productPopup;// The UI panel for this product

    private void OnMouseDown()
    {
        if (productPopup != null)
        {
            productPopup.SetActive(true); // Show the product UI
        }
        else
        {
            Debug.LogError("Product popup is not assigned for " + gameObject.name);
        }
    }
}
