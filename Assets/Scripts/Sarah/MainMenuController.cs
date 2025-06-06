using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour, IPointerClickHandler
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
        SceneManager.LoadScene("SignupScene"); // A scene named "SignupScene" should be existed
    }

    public void OpenLogin()
    {
        SceneManager.LoadScene("LoginScene"); // A scene named "LoginScene" should be existed
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.pointerPress == signupButton.gameObject)
        {
            OpenSignup();
        }
        else if (eventData.pointerPress == loginButton.gameObject)
        {
            OpenLogin();
        }
    }
}

