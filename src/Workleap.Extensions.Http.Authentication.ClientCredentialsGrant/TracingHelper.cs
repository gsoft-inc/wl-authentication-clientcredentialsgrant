using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

internal static class TracingHelper
{
    private const string ActivityName = "ClientCredentials";

    private const string ClientIdTagName = "clientcredentials.clientid";

    private static readonly Assembly Assembly = typeof(TracingHelper).Assembly;
    private static readonly AssemblyName AssemblyName = Assembly.GetName();
    private static readonly string AssemblyVersion = Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? AssemblyName.Version!.ToString();

    private static readonly ActivitySource ActivitySource = new ActivitySource(AssemblyName.Name, AssemblyVersion);

    [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument", Justification = "We want a specific activity name, not the caller method name")]
    public static Activity? StartAuthenticationActivity(string clientId)
    {
        var activity = ActivitySource.StartActivity(ActivityName);

        if (activity != null)
        {
            activity.DisplayName = "Client credentials authenticate";
            activity.SetTag(ClientIdTagName, clientId);
        }

        return activity;
    }

    [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument", Justification = "We want a specific activity name, not the caller method name")]
    public static Activity? StartBackgroundRefreshActivity(string clientId)
    {
        var activity = ActivitySource.StartActivity(ActivityName, ActivityKind.Internal, parentId: null!);

        if (activity != null)
        {
            activity.DisplayName = "Client credentials background refresh";
            activity.SetTag(ClientIdTagName, clientId);
        }

        return activity;
    }
}