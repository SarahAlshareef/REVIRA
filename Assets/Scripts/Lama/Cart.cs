
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class Cart : MonoBehaviour{
    public GameObject storePopup;  // ������� �������� ������ �������
    public Button CancelEnter;      // �� ������� (����� ������)

    void Start()
    {
        // ����� ����� ��� ��� ������� ����� ���� ������ ������� �������� ��� ����� ����
        CancelEnter.onClick.AddListener(HideStorePopUp);

        // ������ �� �� ������� �������� ����� ��� ��� �������
        storePopup.SetActive(false);
    }

    public void HideStorePopUp()
    {
        storePopup.SetActive(false); // ����� ������� ��������
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // ������ ��� ��� ��� �������� �� ��� ��� �� ������� (CancelEnter)
        if (eventData.pointerPress == CancelEnter.gameObject)
        {
            HideStorePopUp();
        }
    }
}