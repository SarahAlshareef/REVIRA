using UnityEngine;
using System.Collections.Generic;

public class ProductUIManager : MonoBehaviour
{
    public static ProductUIManager Instance;

    [System.Serializable]
    public class ProductPanelPair
    {
        public string productID;         
        public GameObject panel;         
    }

    [Header("Product Panels List")]
    public List<ProductPanelPair> panels = new List<ProductPanelPair>();

    [Header("Distance From Camera")]
    public float distanceFromCamera = 2f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ShowProductPanel(string productID)
    {
        HideAllPanels();

        foreach (var pair in panels)
        {
            if (pair.productID == productID)
            {
                GameObject panel = pair.panel;
                panel.SetActive(true);
                PlaceInFrontOfUser(panel);
                return;
            }
        }

        Debug.LogWarning($"No panel found for productID: {productID}");
    }

    private void HideAllPanels()
    {
        foreach (var pair in panels)
        {
            if (pair.panel != null)
                pair.panel.SetActive(false);
        }
    }

    private void PlaceInFrontOfUser(GameObject panel)
    {
        Transform cam = Camera.main.transform;
        Vector3 targetPos = cam.position + cam.forward * distanceFromCamera;
        panel.transform.position = targetPos;
        panel.transform.LookAt(cam);
        panel.transform.Rotate(0, 180, 0);
    }
}