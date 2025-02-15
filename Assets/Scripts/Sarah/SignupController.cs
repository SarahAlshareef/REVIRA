
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SignupController : MonoBehaviour
{

    public Button LoginButton;
   

    void Start()
    {
        LoginButton.onClick.AddListener(OpenLogin);
    }

    public void OpenLogin()
    {
        SceneManager.LoadScene("LoginScene"); // Ensure a scene named "LoginScene" exists
    }
}

