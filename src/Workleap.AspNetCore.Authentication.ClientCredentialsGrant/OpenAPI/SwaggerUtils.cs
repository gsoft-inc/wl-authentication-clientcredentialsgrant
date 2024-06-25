using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Workleap.AspNetCore.Authentication.ClientCredentialsGrant.OpenAPI;

internal static class SwaggerUtils
{
    public static IEnumerable<string> GetRequiredPermissions(ApiDescription apiDescription)
    {
        List<RequireClientCredentialsAttribute> attributes = new();

        if (apiDescription.TryGetMethodInfo(out var methodInfo))
        {
            var hasAllowAnonymous = methodInfo.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any();
            if (hasAllowAnonymous)
            {
                return Enumerable.Empty<string>();
            }
            
            // Controllers - Attributes on the action method (empty for minimal APIs)
            attributes.AddRange(methodInfo.GetCustomAttributes<RequireClientCredentialsAttribute>(inherit: true));
        }

        // Minimal APIs endpoint metadata (empty for controller actions)
        attributes.AddRange(apiDescription.ActionDescriptor.EndpointMetadata.OfType<RequireClientCredentialsAttribute>());

        return attributes.SelectMany(attribute => attribute.RequiredPermissions).Distinct();
    }

    // It assumes the identity provider is supporting the target-entity scope format
    public static string FormatScope(string audience, string permission)
    {
        return $"target-entity:{audience}:{permission}";
    }
    
    public static string GetAllScope(string audience)
    {
        return $"target-entity:{audience}";
    }
}