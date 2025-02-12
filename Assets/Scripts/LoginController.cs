
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginController : MonoBehaviour
{

    public Button SignupButton;

   

    void Start()
    {

        // Assign button click events
        SignupButton.onClick.AddListener(OpenSignup);
     
    }


    public void OpenSignup()
    {
        SceneManager.LoadScene("SignupScene"); // Ensure a scene named "SignupScene" exists
    }

}

