namespace Workleap.Extensions.Http.Authentication.ClientCredentialsGrant;

internal static class TaskExtensions
{
    /// <summary>
    /// Observes the task to avoid the UnobservedTaskException event to be raised.
    /// Full explanation here: https://www.meziantou.net/fire-and-forget-a-task-in-dotnet.htm
    /// </summary>
    public static void Forget(this Task task)
    {
        if (!task.IsCompleted || task.IsFaulted)
        {
            _ = ForgetAwaited(task);
        }

        static async Task ForgetAwaited(Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch
            {
                // Nothing to do here
            }
        }
    }
}