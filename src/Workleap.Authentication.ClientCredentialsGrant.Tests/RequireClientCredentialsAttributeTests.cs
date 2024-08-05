using System.Collections;
using Microsoft.AspNetCore.Authorization;
using Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

namespace Workleap.Authentication.ClientCredentialsGrant.Tests;

public class RequireClientCredentialsAttributeTests
{
    [Theory]
    [ClassData(typeof(ClientCredentialsScopeData))]
    public void GivenAllPossibleClassicScopes_WhenCreate_ThenExpectedPermission(ClientCredentialsScope scope, string expectedPermission)
    {
        var attribute = new RequireClientCredentialsAttribute(scope);
        Assert.Equal(expectedPermission, attribute.RequiredPermission);
    }

    [Fact]
    public void GivenInvalidClassicScope_WhenCreate_ThenThrowArgumentException()
    {
        var scope = (ClientCredentialsScope)999;
        Assert.Throws<ArgumentOutOfRangeException>(() => new RequireClientCredentialsAttribute(scope));
    }
    
    [Fact]
    public void GivenSinglePermission_WhenCreate_ThenSamePermission()
    {
        var expectedPermission = "cocktail.drink";
        var attribute = new RequireClientCredentialsAttribute(expectedPermission);
        Assert.Equal(expectedPermission, attribute.RequiredPermission);
    }

    private sealed class ClientCredentialsScopeData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return [ClientCredentialsScope.Read, "read"];
            yield return [ClientCredentialsScope.Write, "write"];
            yield return [ClientCredentialsScope.Admin, "admin"];
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}