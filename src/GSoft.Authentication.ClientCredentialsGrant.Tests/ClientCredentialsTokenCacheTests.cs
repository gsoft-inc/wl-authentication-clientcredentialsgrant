using System.Collections.Concurrent;
using System.Text.Json;
using FakeItEasy;
using GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GSoft.Authentication.ClientCredentialsGrant.Tests;

// Inspired from Microsoft.Identity.Web caching tests:
// https://github.com/AzureAD/microsoft-identity-web/blob/2.9.0/tests/Microsoft.Identity.Web.Test/L1L2CacheTests.cs
public class ClientCredentialsTokenCacheTests
{
    private const string TestClientName = "client";
    private const string TestCacheKey = "key";

    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _distributedCache;
    private readonly IClientCredentialsTokenSerializer _tokenSerializer;
    private readonly ClientCredentialsTokenCache _tokenCache;

    public ClientCredentialsTokenCacheTests()
    {
        this._distributedCache = A.Fake<IDistributedCache>(x => x.Wrapping(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()))));
        this._memoryCache = A.Fake<IMemoryCache>(x => x.Wrapping(new MemoryCache(new MemoryCacheOptions())));

        this._tokenSerializer = A.Fake<IClientCredentialsTokenSerializer>();
        A.CallTo(() => this._tokenSerializer.Serialize(A<string>._, A<ClientCredentialsToken>._)).ReturnsLazily((string _, ClientCredentialsToken token) => JsonSerializer.SerializeToUtf8Bytes(token));
        A.CallTo(() => this._tokenSerializer.Deserialize(A<string>._, A<byte[]>._)).ReturnsLazily((string _, byte[] bytes) => JsonSerializer.Deserialize<ClientCredentialsToken>(bytes)!);

        var namedOptions = new ConcurrentDictionary<string, ClientCredentialsOptions>(StringComparer.Ordinal)
        {
            [TestClientName] = new ClientCredentialsOptions { CacheKey = TestCacheKey },
        };

        var optionsMonitor = A.Fake<IOptionsMonitor<ClientCredentialsOptions>>();
        A.CallTo(() => optionsMonitor.Get(A<string>._)).ReturnsLazily((string name) => namedOptions.GetOrAdd(name, _ => new ClientCredentialsOptions()));

        this._tokenCache = new ClientCredentialsTokenCache(this._memoryCache, this._distributedCache, this._tokenSerializer, optionsMonitor);
    }

    [Fact]
    public async Task SetAsync_Stores_Token_In_Both_L1_And_L2()
    {
        var token = new ClientCredentialsToken { AccessToken = "accessToken", Expiration = DateTimeOffset.UtcNow };

        await this._tokenCache.SetAsync(TestClientName, token, CancellationToken.None);

        object? ignored;
        A.CallTo(() => this._tokenSerializer.Serialize(TestClientName, A<ClientCredentialsToken>._)).MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => this._memoryCache.CreateEntry(TestCacheKey)).MustHaveHappenedOnceExactly())
            .Then(A.CallTo(() => this._distributedCache.SetAsync(TestCacheKey, A<byte[]>._, A<DistributedCacheEntryOptions>._, CancellationToken.None)).MustHaveHappenedOnceExactly());

        A.CallTo(() => this._memoryCache.TryGetValue(A<string>._, out ignored)).MustNotHaveHappened();
        A.CallTo(() => this._distributedCache.GetAsync(A<string>._, CancellationToken.None)).MustNotHaveHappened();
        A.CallTo(() => this._tokenSerializer.Deserialize(A<string>._, A<byte[]>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GetAsync_When_Both_L1_And_L2_Are_Empty_Returns_Null()
    {
        var actualToken = await this._tokenCache.GetAsync(TestClientName, CancellationToken.None);
        Assert.Null(actualToken);

        object? ignored;
        A.CallTo(() => this._memoryCache.TryGetValue(TestCacheKey, out ignored)).MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => this._distributedCache.GetAsync(TestCacheKey, CancellationToken.None)).MustHaveHappenedOnceExactly());

        A.CallTo(() => this._memoryCache.CreateEntry(A<object>._)).MustNotHaveHappened();
        A.CallTo(() => this._distributedCache.SetAsync(A<string>._, A<byte[]>._, A<DistributedCacheEntryOptions>._, CancellationToken.None)).MustNotHaveHappened();
        A.CallTo(() => this._tokenSerializer.Serialize(A<string>._, A<ClientCredentialsToken>._)).MustNotHaveHappened();
        A.CallTo(() => this._tokenSerializer.Deserialize(A<string>._, A<byte[]>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GetAsync_When_Token_In_L1_And_Empty_L2_Returns_Token_From_L1_Without_Reading_L2()
    {
        var expectedToken = new ClientCredentialsToken { AccessToken = "accessToken", Expiration = DateTimeOffset.UtcNow };
        this.AddTokenToL1Cache(TestCacheKey, expectedToken);

        var actualToken = await this._tokenCache.GetAsync(TestClientName, CancellationToken.None);
        Assert.Equal(expectedToken, actualToken);
        Assert.NotSame(expectedToken, actualToken);

        object? ignored;
        A.CallTo(() => this._memoryCache.TryGetValue(TestCacheKey, out ignored)).MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => this._tokenSerializer.Deserialize(TestClientName, A<byte[]>._)).MustHaveHappenedOnceExactly());

        A.CallTo(() => this._memoryCache.CreateEntry(A<string>._)).MustNotHaveHappened();
        A.CallTo(() => this._distributedCache.GetAsync(A<string>._, CancellationToken.None)).MustNotHaveHappened();
        A.CallTo(() => this._distributedCache.SetAsync(A<string>._, A<byte[]>._, A<DistributedCacheEntryOptions>._, CancellationToken.None)).MustNotHaveHappened();
        A.CallTo(() => this._tokenSerializer.Serialize(A<string>._, A<ClientCredentialsToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GetAsync_When_Token_In_Both_L1_And_L2_Returns_Token_From_L1_Without_Reading_L2()
    {
        var expectedToken = new ClientCredentialsToken { AccessToken = "accessToken", Expiration = DateTimeOffset.UtcNow };
        this.AddTokenToL1Cache(TestCacheKey, expectedToken);
        this.AddTokenToL2Cache(TestCacheKey, expectedToken);

        var actualToken = await this._tokenCache.GetAsync(TestClientName, CancellationToken.None);
        Assert.Equal(expectedToken, actualToken);
        Assert.NotSame(expectedToken, actualToken);

        object? ignored;
        A.CallTo(() => this._memoryCache.TryGetValue(TestCacheKey, out ignored)).MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => this._tokenSerializer.Deserialize(TestClientName, A<byte[]>._)).MustHaveHappenedOnceExactly());

        A.CallTo(() => this._memoryCache.CreateEntry(A<string>._)).MustNotHaveHappened();
        A.CallTo(() => this._distributedCache.GetAsync(A<string>._, CancellationToken.None)).MustNotHaveHappened();
        A.CallTo(() => this._distributedCache.SetAsync(A<string>._, A<byte[]>._, A<DistributedCacheEntryOptions>._, CancellationToken.None)).MustNotHaveHappened();
        A.CallTo(() => this._tokenSerializer.Serialize(A<string>._, A<ClientCredentialsToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task GetAsync_When_Token_In_L2_And_Not_L1_Then_Promote_L2_To_L1_Then_Returns_L2_Token()
    {
        var expectedToken = new ClientCredentialsToken { AccessToken = "accessToken", Expiration = DateTimeOffset.UtcNow };
        this.AddTokenToL2Cache(TestCacheKey, expectedToken);

        var actualToken = await this._tokenCache.GetAsync(TestClientName, CancellationToken.None);
        Assert.Equal(expectedToken, actualToken);
        Assert.NotSame(expectedToken, actualToken);

        object? ignored;
        A.CallTo(() => this._memoryCache.TryGetValue(TestCacheKey, out ignored)).MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => this._distributedCache.GetAsync(TestCacheKey, CancellationToken.None)).MustHaveHappenedOnceExactly())
            .Then(A.CallTo(() => this._tokenSerializer.Deserialize(TestClientName, A<byte[]>._)).MustHaveHappenedOnceExactly())
            .Then(A.CallTo(() => this._memoryCache.CreateEntry(TestCacheKey)).MustHaveHappenedOnceExactly());

        A.CallTo(() => this._distributedCache.SetAsync(A<string>._, A<byte[]>._, A<DistributedCacheEntryOptions>._, CancellationToken.None)).MustNotHaveHappened();
        A.CallTo(() => this._tokenSerializer.Serialize(A<string>._, A<ClientCredentialsToken>._)).MustNotHaveHappened();
    }

    private void AddTokenToL1Cache(string cacheKey, ClientCredentialsToken token)
    {
        this._memoryCache.Set(cacheKey, JsonSerializer.SerializeToUtf8Bytes(token));
        Fake.ClearRecordedCalls(this._memoryCache);
    }

    private void AddTokenToL2Cache(string cacheKey, ClientCredentialsToken token)
    {
        this._distributedCache.Set(cacheKey, JsonSerializer.SerializeToUtf8Bytes(token));
        Fake.ClearRecordedCalls(this._distributedCache);
    }
}