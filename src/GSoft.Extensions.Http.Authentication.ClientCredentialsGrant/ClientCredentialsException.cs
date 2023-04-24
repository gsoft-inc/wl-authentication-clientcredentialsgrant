namespace GSoft.Extensions.Http.Authentication.ClientCredentialsGrant;

public sealed class ClientCredentialsException : Exception
{
    public ClientCredentialsException(string message)
        : base(message)
    {
    }

    public ClientCredentialsException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}