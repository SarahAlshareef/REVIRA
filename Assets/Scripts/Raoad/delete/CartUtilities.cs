

using System;
public static class CartUtilities
{
    // Returns the current timestamp in seconds since Unix epoch
    public static long GetCurrentTimestamp()
    {
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (long)(DateTime.UtcNow - epoch).TotalSeconds;
    }

    // Returns the expiry timestamp for cart items (24 hours from now)
    public static long GetExpiryTimestamp()
    {
        DateTime expiry = DateTime.UtcNow.AddHours(24); // Cart item expires in 24 hours
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return (long)(expiry - epoch).TotalSeconds;
    }
}