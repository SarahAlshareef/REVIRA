// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PasswordVisibility : MonoBehaviour
{
    public TMP_InputField passwordInputField;  
    public Button toggleButton;             
    public Sprite showIcon;                  
    public Sprite hideIcon;                    

    private bool isPasswordVisible = false;

    void Start()
    {
        SetPasswordHidden();
        toggleButton?.onClick.AddListener(TogglePasswordVisibility);
    }

    void TogglePasswordVisibility()
    {
        isPasswordVisible = !isPasswordVisible;

        if (isPasswordVisible)
        {
            passwordInputField.contentType = TMP_InputField.ContentType.Standard;
            toggleButton.image.sprite = hideIcon;
        }
        else
        {
            SetPasswordHidden();
        }
        passwordInputField.ForceLabelUpdate(); 
    }

    void SetPasswordHidden()
    {
        passwordInputField.contentType = TMP_InputField.ContentType.Password;
        toggleButton.image.sprite = showIcon;
    }
}
