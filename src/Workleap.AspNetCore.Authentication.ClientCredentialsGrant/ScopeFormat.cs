namespace Workleap.AspNetCore.Authentication.ClientCredentialsGrant;

public enum ScopeFormat
{
    /// <summary>
    /// Not specified, nor specific to a particular Identity provider (IdP).
    /// </summary>
    Generic = 0,

    /// <summary>
    /// Use FusionAuth's specific scope formatting and handling with entities.
    /// "target-entity:e2a3877b-d132-4bbb-a9b4-651d74fd8a8b:read,write"
    /// </summary>
    FusionAuth = 1,
}