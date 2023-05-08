using GSoft.AspNetCore.Authentication.ClientCredentialsGrant;
using Microsoft.AspNetCore.Authorization;

namespace GSoft.Authentication.ClientCredentialsGrant.Tests;

public class ClientCredentialsAuthorizeAttributeTests
{
    [Theory]
    [InlineData(ClientCredentialsScope.Read, ClientCredentialsDefaults.AuthorizationReadPolicy)]
    [InlineData(ClientCredentialsScope.Write, ClientCredentialsDefaults.AuthorizationWritePolicy)]
    [InlineData(ClientCredentialsScope.Admin, ClientCredentialsDefaults.AuthorizationAdminPolicy)]
    public void Given_Valid_Scope_When_Create_Then_Map_To_Proper_Policy_Name(ClientCredentialsScope scope, string expectedPolicy)
    {
        var attribute = new ClientCredentialsAuthorizeAttribute(scope);
        Assert.Equal(expectedPolicy, attribute.Policy);
        Assert.Equal(scope, attribute.Scope);
    }
}