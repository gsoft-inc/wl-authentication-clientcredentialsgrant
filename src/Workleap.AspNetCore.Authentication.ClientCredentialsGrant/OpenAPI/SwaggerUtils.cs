using System.Reflection;
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
            // Controllers - Attributes on the action method (empty for minimal APIs)
            var methodAttributes = methodInfo.GetCustomAttributes<RequireClientCredentialsAttribute>(inherit: true).ToList();

            if (methodAttributes.Count != 0)
            {
                attributes.AddRange(methodAttributes);
            }
            else if (methodInfo.DeclaringType != null)
            {
                // Controllers - Attributes on the controller class itself if was not overridden by the action method
                attributes.AddRange(methodInfo.DeclaringType.GetCustomAttributes<RequireClientCredentialsAttribute>(inherit: true));
            }
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