using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ExitStorePopup : MonoBehaviour
{
    public GameObject popupPanel;            // ����� ����� ��
    public Button exitStoreButton;           // �� ��� ����� ��
    public Button cancelButton;              // �� ����� ����� ��
    public Button confirmExitButton;         // �� "Exit Store" ���� ����� ��

    public string targetSceneName = "StoreSelection";  // ��� ������ ���� ������ ��

    void Start()
    {
        popupPanel.SetActive(false);

        exitStoreButton.onClick.AddListener(ShowPopup);
        cancelButton.onClick.AddListener(HidePopup);
        confirmExitButton.onClick.AddListener(LoadExitScene);
    }

    void ShowPopup()
    {
        popupPanel.SetActive(true);
    }

    void HidePopup()
    {
        popupPanel.SetActive(false);
    }

    void LoadExitScene()
    {
        SceneManager.LoadScene(targetSceneName);
    }
}