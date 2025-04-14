// Unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
// C#
using System.Collections;
using System.Collections.Generic;

public class OpenProfile : MonoBehaviour
{
    public Button ProfileButton;
    void Start()
    {
        ProfileButton?.onClick.AddListener(OpenProfileScene);
    }
    void OpenProfileScene()
    {
        SceneTracker.Instance.SetPreviousScene(SceneManager.GetActiveScene().name);
        SceneManager.LoadScene("ViewProfile");
    }
}
