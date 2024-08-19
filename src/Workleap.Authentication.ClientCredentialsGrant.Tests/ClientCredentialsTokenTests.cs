using System.Globalization;
using Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

namespace Workleap.Authentication.ClientCredentialsGrant.Tests;

public class ClientCredentialsTokenTests
{
    [Theory]
    [InlineData("2024-01-10", "2024-01-09", 0)]
    [InlineData("2024-01-10", "2024-01-11", 1)]
    public void GetTimeToLive_Works(string nowStr, string expirationStr, int expectedTimeToLiveInDays)
    {
        var now = DateTimeOffset.ParseExact(nowStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var expiration = DateTimeOffset.ParseExact(expirationStr, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        var token = new ClientCredentialsToken("dummy", expiration);
        Assert.Equal(TimeSpan.FromDays(expectedTimeToLiveInDays), token.GetTimeToLive(now));
    }
}