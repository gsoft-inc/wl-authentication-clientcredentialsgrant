#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Authorization;

public enum ClientCredentialsScope
{
    Read = 0,
    Write = 1,
    Admin = 2,
}