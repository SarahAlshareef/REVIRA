//Unity
using UnityEngine;
using UnityEngine.UI;

public class Profile : MonoBehaviour
{
    public GameObject profilePanel;
    public Button openProfileButton;
    public Button closeProfileButton;

  
    void Start()
    {
        if (profilePanel != null)
            profilePanel.SetActive(false);

        openProfileButton?.onClick.AddListener(ShowProfile);
        closeProfileButton?.onClick.AddListener(HideProfile);
    }

    void ShowProfile()
    {
        profilePanel.SetActive(true);
    }

    void HideProfile()
    {
        profilePanel.SetActive(false);
    }
}

