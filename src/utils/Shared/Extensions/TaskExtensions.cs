namespace Shared.Extensions;

public static class TaskExtensions
{
    public static Task RepeatWhileTaskRunning(this Task parent, Action action, int delayInMilliseconds = 500)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(action);

        Task.Run(() =>
        {
            while (!parent.IsCompleted)
            {
                action();
                Task.Delay(delayInMilliseconds).Wait();
            }
        });

        return parent;
    }
    public static T AwaitResult<T>(this Task<T> task) => task.GetAwaiter().GetResult();
}
