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
    public string Gender { get; private set; }
    public string PhoneNumber { get; private set; }

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
    public void SetUserData(string userId, string firstName, string lastName, string email, float accountBalance, string gender, string phoneNumber)
    {
        UserId = userId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        AccountBalance = accountBalance;
        Gender = gender;
        PhoneNumber = phoneNumber;
    }
    public void UpdateFirstName(string newFirstName)
    {
        FirstName = newFirstName;
    }
    public void UpdateLastName(string newLastName)
    {
        LastName = newLastName;
    }
    public void UpdateEmail(string newEmail)
    {
        Email = newEmail;
    }
    public void UpdateAccountBalance(float newBalance)
    {
        AccountBalance = newBalance;
    }
    public void UpdateGender(string newGender)
    {
        Gender = newGender;
    }

    public void UpdatePhoneNumber(string newPhoneNumber)
    {
        PhoneNumber = newPhoneNumber;
    }
}

