using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine.Networking;

public class SignUpREST : MonoBehaviour, IPointerClickHandler
{
    public TMP_InputField firstNameInput, lastNameInput, emailInput, passwordInput;
    public Button signUpButton, loginButton;
    public TextMeshProUGUI errorText;

    private const string API_KEY = "AIzaSyDRgLSCLzuATXV8Kis_CW7UiEgeliv2t1k";
    private const string DATABASE_URL = "https://fir-unity-29721-default-rtdb.firebaseio.com";

    void Start()
    {
        signUpButton.onClick.AddListener(OnSignUpButtonClick);
        loginButton.onClick.AddListener(() => SceneManager.LoadScene("LoginScene"));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnSignUpButtonClick();
    }

    void OnSignUpButtonClick()
    {
        string firstName = firstNameInput.text.Trim();
        string lastName = lastNameInput.text.Trim();
        string email = emailInput.text.Trim();
        string password = passwordInput.text;

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Please fill all fields.");
            return;
        }

        StartCoroutine(SignUpUser(firstName, lastName, email, password));
    }

    IEnumerator SignUpUser(string firstName, string lastName, string email, string password)
    {
        string signUpUrl = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={API_KEY}";
        var signUpData = new
        {
            email = email,
            password = password,
            returnSecureToken = true
        };

        string jsonData = JsonUtility.ToJson(signUpData);
        var request = new UnityWebRequest(signUpUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            ShowError("Signup failed. Try again");
            Debug.LogError("Signup failed: " + request.error);
            yield break;
        }

        var responseText = request.downloadHandler.text;
        var authResponse = JsonUtility.FromJson<AuthResponse>(responseText);
        Debug.Log("Signup successful: " + authResponse.localId);

        yield return StartCoroutine(SaveUserToDatabase(authResponse.localId, authResponse.idToken, firstName, lastName, email));
    }

    IEnumerator SaveUserToDatabase(string userId, string idToken, string firstName, string lastName, string email)
    {
        string url = $"{DATABASE_URL}/users/{userId}.json?auth={idToken}";

        var userData = new UserData
        {
            userId = userId,
            firstName = firstName,
            lastName = lastName,
            email = email,
            accountBalance = 1000
        };

        string jsonData = JsonUtility.ToJson(userData);
        var request = new UnityWebRequest(url, "PUT");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            ShowError("Failed to save user data");
            Debug.LogError("Database error: " + request.error);
            yield break;
        }

        SceneManager.LoadScene("LoginScene");
    }

    void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.color = Color.red;
        }
        Debug.LogError(message);
    }

    [Serializable]
    public class AuthResponse
    {
        public string idToken;
        public string localId;
        public string email;
    }

    [Serializable]
    public class UserData
    {
        public string userId;
        public string firstName;
        public string lastName;
        public string email;
        public int accountBalance;
    }
}