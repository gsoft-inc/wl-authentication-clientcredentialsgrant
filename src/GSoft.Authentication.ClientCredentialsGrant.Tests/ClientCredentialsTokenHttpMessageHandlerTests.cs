using System.Net;
using FakeItEasy;

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant.Tests;

public sealed class ClientCredentialsTokenHttpMessageHandlerTests : IDisposable
{
    private const string TestClientName = "client";
    private const string TestAccessToken = "accessToken";

    private readonly IClientCredentialsTokenManagementService _tokenManagementService;
    private readonly MockPrimaryHttpMessageHandler _mockPrimaryHttpMessageHandler;
    private readonly HttpClient _clientCredentialsHttpClient;

    public ClientCredentialsTokenHttpMessageHandlerTests()
    {
        this._tokenManagementService = A.Fake<IClientCredentialsTokenManagementService>();
        A.CallTo(() => this._tokenManagementService.GetAccessTokenAsync(TestClientName, A<CachingBehavior>._, A<CancellationToken>._))
            .Returns(Task.FromResult(new ClientCredentialsToken { AccessToken = TestAccessToken, Expiration = DateTimeOffset.UtcNow }));

        this._mockPrimaryHttpMessageHandler = new MockPrimaryHttpMessageHandler();

        var clientCredentialsTokenHandler = new ClientCredentialsTokenHttpMessageHandler(this._tokenManagementService, TestClientName)
        {
            InnerHandler = this._mockPrimaryHttpMessageHandler,
        };

        this._clientCredentialsHttpClient = new HttpClient(clientCredentialsTokenHandler);
    }

    [Fact]
    public async Task SendAsync_When_First_Response_Is_Ok_Returns_Ok_And_Skips_Second_Retry()
    {
        this._mockPrimaryHttpMessageHandler.ExpectedHttpResponseMessages = new[]
        {
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("Access granted on first try") },
        };

        var result = await this._clientCredentialsHttpClient.GetStringAsync("https://whatever", CancellationToken.None);
        Assert.Equal("Access granted on first try", result);

        A.CallTo(() => this._tokenManagementService.GetAccessTokenAsync(TestClientName, CachingBehavior.PreferCache, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => this._tokenManagementService.GetAccessTokenAsync(TestClientName, CachingBehavior.ForceRefresh, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task SendAsync_When_First_Response_Is_NoContent_Returns_NoContent_And_Skips_Second_Retry()
    {
        this._mockPrimaryHttpMessageHandler.ExpectedHttpResponseMessages = new[]
        {
            new HttpResponseMessage(HttpStatusCode.NoContent),
        };

        var response = await this._clientCredentialsHttpClient.GetAsync("https://whatever", CancellationToken.None);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        A.CallTo(() => this._tokenManagementService.GetAccessTokenAsync(TestClientName, CachingBehavior.PreferCache, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => this._tokenManagementService.GetAccessTokenAsync(TestClientName, CachingBehavior.ForceRefresh, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task SendAsync_When_Unauthorized_Followed_By_Ok_Returns_Ok()
    {
        this._mockPrimaryHttpMessageHandler.ExpectedHttpResponseMessages = new[]
        {
            CreateHttpResponseMessage(HttpStatusCode.Unauthorized, "Token was rejected"),
            CreateHttpResponseMessage(HttpStatusCode.OK, "Access granted on second attempt"),
        };

        var result = await this._clientCredentialsHttpClient.GetStringAsync("https://whatever", CancellationToken.None);
        Assert.Equal("Access granted on second attempt", result);

        A.CallTo(() => this._tokenManagementService.GetAccessTokenAsync(TestClientName, CachingBehavior.PreferCache, A<CancellationToken>._)).MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => this._tokenManagementService.GetAccessTokenAsync(TestClientName, CachingBehavior.ForceRefresh, A<CancellationToken>._)).MustHaveHappenedOnceExactly());
    }

    [Fact]
    public async Task SendAsync_When_Unauthorized_Followed_By_Unauthorized_Returns_Unauthorized()
    {
        this._mockPrimaryHttpMessageHandler.ExpectedHttpResponseMessages = new[]
        {
            CreateHttpResponseMessage(HttpStatusCode.Unauthorized, "Token was rejected"),
            CreateHttpResponseMessage(HttpStatusCode.Unauthorized, "Token was rejected again"),
        };

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => this._clientCredentialsHttpClient.GetStringAsync("https://whatever", CancellationToken.None));
        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);

        A.CallTo(() => this._tokenManagementService.GetAccessTokenAsync(TestClientName, CachingBehavior.PreferCache, A<CancellationToken>._)).MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => this._tokenManagementService.GetAccessTokenAsync(TestClientName, CachingBehavior.ForceRefresh, A<CancellationToken>._)).MustHaveHappenedOnceExactly());
    }

    [Fact]
    public async Task SendAsync_When_Unauthorized_Followed_By_Unauthorized_Returns_Unauthorized_And_Skips_Third_Retry()
    {
        this._mockPrimaryHttpMessageHandler.ExpectedHttpResponseMessages = new[]
        {
            CreateHttpResponseMessage(HttpStatusCode.Unauthorized, "Token was rejected"),
            CreateHttpResponseMessage(HttpStatusCode.Unauthorized, "Token was rejected again"),
            CreateHttpResponseMessage(HttpStatusCode.OK, "We won't retry 3 times"),
        };

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => this._clientCredentialsHttpClient.GetStringAsync("https://whatever", CancellationToken.None));
        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);

        A.CallTo(() => this._tokenManagementService.GetAccessTokenAsync(TestClientName, CachingBehavior.PreferCache, A<CancellationToken>._)).MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => this._tokenManagementService.GetAccessTokenAsync(TestClientName, CachingBehavior.ForceRefresh, A<CancellationToken>._)).MustHaveHappenedOnceExactly());
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task SendAsync_When_Unexpected_Response_Other_Than_Unauthorized_Returns_Unexpected_Response_And_Skips_Second_Retry(HttpStatusCode nonRetriableStatusCode)
    {
        this._mockPrimaryHttpMessageHandler.ExpectedHttpResponseMessages = new[]
        {
            CreateHttpResponseMessage(nonRetriableStatusCode, "Something wrong happened"),
        };

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => this._clientCredentialsHttpClient.GetStringAsync("https://whatever", CancellationToken.None));
        Assert.Equal(nonRetriableStatusCode, ex.StatusCode);

        A.CallTo(() => this._tokenManagementService.GetAccessTokenAsync(TestClientName, CachingBehavior.PreferCache, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => this._tokenManagementService.GetAccessTokenAsync(TestClientName, CachingBehavior.ForceRefresh, A<CancellationToken>._)).MustNotHaveHappened();
    }

    private static HttpResponseMessage CreateHttpResponseMessage(HttpStatusCode statusCode, string content)
    {
        return new HttpResponseMessage(statusCode) { Content = new StringContent(content) };
    }

    public void Dispose()
    {
        this._mockPrimaryHttpMessageHandler.Dispose();
        this._clientCredentialsHttpClient.Dispose();
    }

    private sealed class MockPrimaryHttpMessageHandler : DelegatingHandler
    {
        private int _currentResponseIndex;

        public MockPrimaryHttpMessageHandler()
        {
            this.ExpectedHttpResponseMessages = Array.Empty<HttpResponseMessage>();
            this._currentResponseIndex = 0;
        }

        public HttpResponseMessage[] ExpectedHttpResponseMessages { private get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(TestAccessToken, request.Headers.Authorization?.Parameter);
            return Task.FromResult(this.ExpectedHttpResponseMessages[this._currentResponseIndex++]);
        }
    }
}