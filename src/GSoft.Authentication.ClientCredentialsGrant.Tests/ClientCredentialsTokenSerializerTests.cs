using System.Globalization;
using System.Security.Cryptography;
using FakeItEasy;
using Microsoft.AspNetCore.DataProtection;

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant.Tests;

public class ClientCredentialsTokenSerializerTests
{
    private const string TestClientName = "client";

    // This is the base64 representation from the JSON-encoded bytes of the access token below
    private const string ExpectedSerializedTokenBase64 = "eyJhY2Nlc3NUb2tlbiI6ImFjY2Vzc1Rva2VuIiwiZXhwaXJhdGlvbiI6IjIwMjMtMDQtMjVUMDM6MTc6NDcuOTMzMTE1MyswMDowMCJ9";

    private static readonly byte[] ExpectedSerializedToken = Convert.FromBase64String(ExpectedSerializedTokenBase64);

    private static readonly ClientCredentialsToken ExpectedDeserializedToken = new ClientCredentialsToken
    {
        AccessToken = "accessToken",
        Expiration = DateTimeOffset.ParseExact("2023-04-25T03:17:47.9331153+00:00", "O", CultureInfo.InvariantCulture),
    };

    private readonly IDataProtector _dataProtector;
    private readonly ClientCredentialsTokenSerializer _tokenSerializer;

    public ClientCredentialsTokenSerializerTests()
    {
        // Configure data protector to return the input without any modification
        this._dataProtector = A.Fake<IDataProtector>();
        A.CallTo(() => this._dataProtector.Protect(A<byte[]>._)).ReturnsLazily((byte[] bytes) => bytes);
        A.CallTo(() => this._dataProtector.Unprotect(A<byte[]>._)).ReturnsLazily((byte[] bytes) => bytes);
        A.CallTo(() => this._dataProtector.CreateProtector(A<string>._))
            .Throws(() => new InvalidOperationException("This method should not be called"));

        var dataProtectionProvider = A.Fake<IDataProtectionProvider>();
        A.CallTo(() => dataProtectionProvider.CreateProtector(ClientCredentialsConstants.DataProtectionPurpose))
            .Returns(this._dataProtector);

        this._tokenSerializer = new ClientCredentialsTokenSerializer(dataProtectionProvider);
        A.CallTo(() => dataProtectionProvider.CreateProtector(ClientCredentialsConstants.DataProtectionPurpose))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void Serialize_When_Successful_Returns_Protected_Bytes()
    {
        var serializedBytes = this._tokenSerializer.Serialize(TestClientName, ExpectedDeserializedToken);

        Assert.Equal(ExpectedSerializedToken, serializedBytes);
        A.CallTo(() => this._dataProtector.Protect(A<byte[]>.That.IsSameSequenceAs(ExpectedSerializedToken))).MustHaveHappenedOnceExactly();
        A.CallTo(() => this._dataProtector.Unprotect(A<byte[]>._)).MustNotHaveHappened();
    }

    [Fact]
    public void Serialize_Wraps_Exception_Into_ClientCredentialsException()
    {
        var protectException = new CryptographicException("Protect failed for some reason");
        A.CallTo(() => this._dataProtector.Protect(A<byte[]>.That.IsSameSequenceAs(ExpectedSerializedToken))).Throws(protectException);

        var actualException = Assert.Throws<ClientCredentialsException>(() => this._tokenSerializer.Serialize(TestClientName, ExpectedDeserializedToken));

        Assert.Equal(protectException, actualException.InnerException);
        Assert.Contains(TestClientName, actualException.Message);
        A.CallTo(() => this._dataProtector.Protect(A<byte[]>.That.IsSameSequenceAs(ExpectedSerializedToken))).MustHaveHappenedOnceExactly();
        A.CallTo(() => this._dataProtector.Unprotect(A<byte[]>._)).MustNotHaveHappened();
    }

    [Fact]
    public void Deserialize_When_Successful_Returns_Unprotected_Token()
    {
        var deserializedToken = this._tokenSerializer.Deserialize(TestClientName, ExpectedSerializedToken);

        Assert.Equal(ExpectedDeserializedToken, deserializedToken);
        A.CallTo(() => this._dataProtector.Protect(A<byte[]>._)).MustNotHaveHappened();
        A.CallTo(() => this._dataProtector.Unprotect(A<byte[]>.That.IsSameSequenceAs(ExpectedSerializedToken))).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void Deserialize_Wraps_Exception_Into_ClientCredentialsException()
    {
        var protectException = new CryptographicException("Unprotect failed for some reason");
        A.CallTo(() => this._dataProtector.Unprotect(A<byte[]>.That.IsSameSequenceAs(ExpectedSerializedToken))).Throws(protectException);

        var actualException = Assert.Throws<ClientCredentialsException>(() => this._tokenSerializer.Deserialize(TestClientName, ExpectedSerializedToken));

        Assert.Equal(protectException, actualException.InnerException);
        Assert.Contains(TestClientName, actualException.Message);
        A.CallTo(() => this._dataProtector.Protect(A<byte[]>._)).MustNotHaveHappened();
        A.CallTo(() => this._dataProtector.Unprotect(A<byte[]>.That.IsSameSequenceAs(ExpectedSerializedToken))).MustHaveHappenedOnceExactly();
    }
}