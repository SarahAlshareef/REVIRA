// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class OpenProfile : MonoBehaviour
{
    public Button ProfileButton;
    void Start()
    {
        ProfileButton?.onClick.AddListener(OpenProfileScene);
    }
    public void OpenProfileScene()
    {
        SceneTracker.Instance.SetPreviousScene(SceneManager.GetActiveScene().name);
        SceneManager.LoadScene("ViewProfile");
    }
}
