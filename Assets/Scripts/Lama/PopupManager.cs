using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ExitStorePopup : MonoBehaviour
{
    public GameObject popupPanel;            // ‰«›–… «·»Ê» √»
    public Button exitStoreButton;           // “— › Õ «·»Ê» √»
    public Button cancelButton;              // “— ≈€·«ﬁ «·»Ê» √»
    public Button confirmExitButton;         // “— "Exit Store" œ«Œ· «·»Ê» √»

    public string targetSceneName = "StoreSelection";  // «”„ «·„‘Âœ «··Ì  —ÊÕÌ‰ ·Â

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