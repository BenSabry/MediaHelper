using System.Collections.Concurrent;

namespace MediaOrganizer;
public static class Exetnsions
{
    #region IEnumerable
    public static IEnumerable<T> SelectMany<T>(this IEnumerable<IEnumerable<T>> source) => source.SelectMany(a => a.Select(b => b));

    public static void ParallelForEach<TInput, TArg>(this IEnumerable<TInput> source, int parallelTasksCount, Action<TInput, long, TArg> action, Func<TArg> getPerTaskArgument, Action<long, long> progressReportAction, int delayInMilliseconds = 500)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(getPerTaskArgument);

        if (parallelTasksCount < 1) return;
        if (delayInMilliseconds < 100) delayInMilliseconds = 100;

        //Changed from List to ConcurrentQueue to auto dispose object after usage
        var queues = new ConcurrentQueue<TInput>[parallelTasksCount].Fill();
        var totalEnumurationCount = 0;
        var enumerator = Task.Run(() =>
        {
            foreach (var item in source)
            {
                queues[totalEnumurationCount % parallelTasksCount].Enqueue(item);
                Interlocked.Increment(ref totalEnumurationCount);
            }
        });

        var args = new TArg[parallelTasksCount];
        var tasks = new Task[parallelTasksCount];
        var currentEnumerationIndex = 0;

        for (int parallelTaskIndex = 0; parallelTaskIndex < parallelTasksCount; parallelTaskIndex++)
        {
            var taskIndex = parallelTaskIndex;
            args[taskIndex] = getPerTaskArgument();
            tasks[taskIndex] = Task.Run(() =>
            {
                while (!enumerator.IsCompleted || !queues[taskIndex].IsEmpty)
                {
                    Task.Delay(delayInMilliseconds).Wait();
                    while (queues[taskIndex % parallelTasksCount].TryDequeue(out var item))
                    {
                        action(item, taskIndex, args[taskIndex % parallelTasksCount]);
                        taskIndex += parallelTasksCount;
                        Interlocked.Increment(ref currentEnumerationIndex);
                    }
                }
            });
        }

        if (progressReportAction is not null)
            Task.Run(() =>
            {
                while (Array.Exists(tasks, i => !i.IsCompleted))
                {
                    progressReportAction(currentEnumerationIndex, totalEnumurationCount);
                    Task.Delay(delayInMilliseconds).Wait();
                }
            }).Wait();

        enumerator.Wait();
        enumerator.Dispose();

        Task.WaitAll(tasks);
    }

    public static Task ParallelForEachTask<TInput, TArg>(this IEnumerable<TInput> source, int parallelTasksCount, Action<TInput, long, TArg> action, Func<TArg> getPerTaskArgument, Action<long, long> progressReportAction, int delayInMilliseconds = 500)
    => Task.Run(() => ParallelForEach(source, parallelTasksCount, action, getPerTaskArgument, progressReportAction, delayInMilliseconds));
    #endregion

    #region Task
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
    #endregion

    #region Array
    public static T[] Fill<T>(this T[] array) where T : new()
    {
        ArgumentNullException.ThrowIfNull(array);

        for (int i = 0; i < array.Length; i++)
            array[i] = new T();

        return array;
    }
    #endregion

    #region Char
    public static bool IsNumber(this char c) => c >= '0' && c <= '9';
    public static bool IsLetter(this char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
    public static bool NotNumberOrLetter(this char c) => !IsNumber(c) && !IsLetter(c);
    #endregion
}
