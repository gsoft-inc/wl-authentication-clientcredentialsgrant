using Microsoft.Extensions.DependencyInjection;

namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

public static class HttpClientBuilderExtentions
{
    public static IHttpClientBuilder AddClientCredentialsHandler(this IHttpClientBuilder builder, Action<ClientCredentialsOptions>? configure = null)
    {
        builder.Services.AddOptions<ClientCredentialsOptions>(builder.Name)
            .BindConfiguration($"{ClientCredentialsOptions.BaseSectionName}:{builder.Name}");

        if (configure != null)
        {
            builder.Services.Configure(builder.Name, configure);
        }

        return builder;
    }
}