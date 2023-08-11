using System.Collections;
using Microsoft.AspNetCore.Authorization;

namespace Workleap.Authentication.ClientCredentialsGrant.Tests;

public class ClientCredentialsAuthorizeAttributeTests
{
    [Theory]
    [ClassData(typeof(ClientCredentialsScopeData))]
    public void GivenAllPossibleScopes_WhenCreate_ThenMappedToPolicy(ClientCredentialsScope scope)
    {
        var attribute = new ClientCredentialsAuthorizeAttribute(scope);
        Assert.Equal(scope, attribute.Scope);
        Assert.NotNull(attribute.Policy);
    }

    [Fact]
    public void GivenInValidScope_WhenCreate_ThenMapToProperPolicyName()
    {
        var scope = (ClientCredentialsScope)999;
        Assert.Throws<ArgumentException>(() => new ClientCredentialsAuthorizeAttribute(scope));
    }

    private sealed class ClientCredentialsScopeData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            foreach (var value in Enum.GetValues<ClientCredentialsScope>())
            {
                yield return new object[] { value };
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}