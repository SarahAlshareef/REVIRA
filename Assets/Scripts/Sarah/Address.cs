[System.Serializable]
public class Address
{
    public string addressName;
    public string country;
    public string city;
    public string district;
    public string street;
    public string building;
    public string phoneNumber;

    public Address(string addressName, string country, string city, string district, string street, string building, string phoneNumber)
    {
        this.addressName = addressName;
        this.country = country;
        this.city = city;
        this.district = district;
        this.street = street;
        this.building = building;
        this.phoneNumber = phoneNumber;
    }
}
