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
        var permission = Assert.Single(attribute.RequiredPermissions);
        Assert.Equal(expectedPermission, permission);
    }

    [Fact]
    public void GivenInvalidClassicScope_WhenCreate_ThenThrowArgumentException()
    {
        var scope = (ClientCredentialsScope)999;
        Assert.Throws<ArgumentException>(() => new RequireClientCredentialsAttribute(scope));
    }
    
    [Fact]
    public void GivenSinglePermission_WhenCreate_ThenSamePermission()
    {
        var expectedPermission = "cocktail.drink";
        var attribute = new RequireClientCredentialsAttribute(expectedPermission);
        var permission = Assert.Single(attribute.RequiredPermissions);
        Assert.Equal(expectedPermission, permission);
    }
    
    [Fact]
    public void GivenMultiplePermission_WhenCreate_ThenSamePermissions()
    {
        var expectedPermissions = new[] { "cocktail.drink", "cocktail.make", "cocktail.buy" };
        var attribute = new RequireClientCredentialsAttribute(expectedPermissions.First(), expectedPermissions.Skip(1).ToArray());
        Assert.True(attribute.RequiredPermissions.SetEquals(expectedPermissions));
    }

    private sealed class ClientCredentialsScopeData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { ClientCredentialsScope.Read, "read" };
            yield return new object[] { ClientCredentialsScope.Write, "write" };
            yield return new object[] { ClientCredentialsScope.Admin, "admin" };
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}