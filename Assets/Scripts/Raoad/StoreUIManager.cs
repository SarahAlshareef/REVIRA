using UnityEngine;

public class StoreUIManager : MonoBehaviour
{
    public static StoreUIManager Instance;

    [Header("Panels")]
    public GameObject productSelectionPanel;
    public GameObject productDetailsPanel;
    public GameObject cartPanel;
    public GameObject promoPanel;
    public GameObject checkoutPanel;

    private void Awake()
    {
        Instance = this;
        ShowOnly(productSelectionPanel); // √Ê· Ê«ÃÂ…  ŸÂ—
    }

    public void ShowOnly(GameObject panelToShow)
    {
        GameObject[] allPanels = { productSelectionPanel, productDetailsPanel, cartPanel, promoPanel, checkoutPanel };
        foreach (GameObject panel in allPanels)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        if (panelToShow != null)
            panelToShow.SetActive(true);
    }

    public void ShowProductDetails(string productID)
    {
        ShowOnly(productDetailsPanel);
        productDetailsPanel.GetComponent<ProductDetailsPanel>().LoadProduct(productID);
    }

    public void GoToCart()
    {
        ShowOnly(cartPanel);
    }

    public void GoToPromo()
    {
        ShowOnly(promoPanel);
    }

    public void GoToCheckout()
    {
        ShowOnly(checkoutPanel);
    }

    public void BackToSelection()
    {
        ShowOnly(productSelectionPanel);
    }
}
