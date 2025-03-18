
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class Cart : MonoBehaviour{
    public GameObject storePopup;  // ÇáäÇİĞÉ ÇáãäÈËŞÉ ÇáÎÇÕÉ ÈÇáãÊÌÑ
    public Button CancelEnter;      // ÒÑ ÇáÅÛáÇŞ (ÅáÛÇÁ ÇáÏÎæá)

    void Start()
    {
        // ÊÚííä ÇáÍÏË ÚäÏ ÈÏÁ ÇáÊÔÛíá áíŞæã ÇáÒÑ ÈÅÎİÇÁ ÇáäÇİĞÉ ÇáãäÈËŞÉ ÚäÏ ÇáäŞÑ Úáíå
        CancelEnter.onClick.AddListener(HideStorePopUp);

        // ÇáÊÃßÏ ãä Ãä ÇáäÇİĞÉ ÇáãäÈËŞÉ ãÎİíÉ ÚäÏ ÈÏÁ ÇáÊÔÛíá
        storePopup.SetActive(false);
    }

    public void HideStorePopUp()
    {
        storePopup.SetActive(false); // ÅÎİÇÁ ÇáäÇİĞÉ ÇáãäÈËŞÉ
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // ÇáÊÍŞŞ ããÇ ÅĞÇ ßÇä ÇáãÓÊÎÏã ŞÏ ÖÛØ Úáì ÒÑ ÇáÅÛáÇŞ (CancelEnter)
        if (eventData.pointerPress == CancelEnter.gameObject)
        {
            HideStorePopUp();
        }
    }
}