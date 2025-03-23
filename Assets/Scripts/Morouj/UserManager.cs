using System.Collections.Generic;
using UnityEngine;

public class UserManager : MonoBehaviour
{
    public static UserManager Instance { get; private set; }

    public string UserId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public float AccountBalance { get; private set; }


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void SetUserData(string userId, string firstName, string lastName, string email, float accountBalance)
    {
        UserId = userId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        AccountBalance = accountBalance;
    }

    public void UpdateAccountBalance(float newBalance)
    {
        AccountBalance = newBalance;
    }
}
