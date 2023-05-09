using System.Collections;
using Microsoft.AspNetCore.Authorization;

namespace GSoft.Authentication.ClientCredentialsGrant.Tests;

public class ClientCredentialsAuthorizeAttributeTests
{
    [Theory]
    [ClassData(typeof(ClientCredentialsScopeData))]
    public void Given_All_Possible_Scopes_When_Create_Then_Mapped_To_Policy(ClientCredentialsScope scope)
    {
        var attribute = new ClientCredentialsAuthorizeAttribute(scope);
        Assert.Equal(scope, attribute.Scope);
        Assert.NotNull(attribute.Policy);
    }

    [Fact]
    public void Given_InValid_Scope_When_Create_Then_Map_To_Proper_Policy_Name()
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