
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{

    public Button signupButton;
    public Button loginButton;
   

    void Start()
    {

        // Assign button click events
        signupButton.onClick.AddListener(OpenSignup);
        loginButton.onClick.AddListener(OpenLogin);
    }


    public void OpenSignup()
    {
        SceneManager.LoadScene("SignupScene"); // Ensure a scene named "SignupScene" exists
    }

    public void OpenLogin()
    {
        SceneManager.LoadScene("LoginScene"); // Ensure a scene named "LoginScene" exists
    }
}

