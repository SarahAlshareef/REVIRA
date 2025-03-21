[System.Serializable]
public class Address
{
    public string addressName;
    public string city;
    public string street;
    public string building;
    public string zipCode;
    public string phoneNumber;

    public Address() { } // Required for Firebase deserialization

    public Address(string addressName, string city, string street, string building, string zipCode, string phoneNumber)
    {
        this.addressName = addressName;
        this.city = city;
        this.street = street;
        this.building = building;
        this.zipCode = zipCode;
        this.phoneNumber = phoneNumber;
    }
}
