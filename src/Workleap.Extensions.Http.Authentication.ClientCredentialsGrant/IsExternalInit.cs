#if !NET5_0_OR_GREATER

using System.ComponentModel;

// ReSharper disable once CheckNamespace
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Runtime.CompilerServices;

// Compiler helper class that allows using "init" keyword in .NET Standard 2.0:
// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/init
// The same thing is also done in ASP.NET Core:
// https://github.com/dotnet/aspnetcore/blob/v6.0.0/src/Shared/IsExternalInit.cs
[EditorBrowsable(EditorBrowsableState.Never)]
internal static class IsExternalInit
{
}
#endif