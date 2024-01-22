using System.Collections.Concurrent;

namespace Shared.Extensions;

public static class IEnumerableExtensions
{
    public static IEnumerable<T> SelectMany<T>(this IEnumerable<IEnumerable<T>> source) => source.SelectMany(a => a.Select(b => b));

    public static TOutput[] SelectParallel<TInput, TOutput>(this IList<TInput> source, Func<TInput, TOutput> function, int parallelTasksCount = 100)
    {
        ArgumentNullException.ThrowIfNull(source);

        var result = new TOutput[source.Count];
        Parallel.For(default, source.Count,
            new ParallelOptions { MaxDegreeOfParallelism = parallelTasksCount },
            (index) => { result[index] = function(source[index]); });

        return result;
    }

    public static void ParallelForEach<TInput, TArg>(
        this IEnumerable<TInput> source,
        int parallelTasksCount,
        Action<TInput, long, TArg> action,
        Func<TArg> getPerTaskArgument,
        Action<long, long>? progressReportAction = null,
        int delayInMilliseconds = 500)
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
                queues[GetIteratedIndex(totalEnumurationCount, parallelTasksCount)].Enqueue(item);
                Interlocked.Increment(ref totalEnumurationCount);
            }
        });

        var tasks = new Task[parallelTasksCount];
        var currentEnumerationIndex = 0;

        for (int taskIndex = 0; taskIndex < parallelTasksCount; taskIndex++)
        {
            var index = taskIndex;
            tasks[index] = Task.Run(() =>
            {
                var arg = getPerTaskArgument();
                while (!enumerator.IsCompleted || !queues[GetIteratedIndex(index, parallelTasksCount)].IsEmpty)
                {
                    Task.Delay(delayInMilliseconds).Wait();
                    while (queues[GetIteratedIndex(index, parallelTasksCount)].TryDequeue(out var item))
                    {
                        action(item, index, arg);
                        index += parallelTasksCount;
                        Interlocked.Increment(ref currentEnumerationIndex);
                    }
                }

                arg.Dispose();
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
        Task.WaitAll(tasks);
    }

    public static Task ParallelForEachAsync<TInput, TArg>(this IEnumerable<TInput> source, int parallelTasksCount, Action<TInput, long, TArg> action, Func<TArg> getPerTaskArgument, Action<long, long>? progressReportAction = null, int delayInMilliseconds = 500)
    => Task.Run(() => ParallelForEach(source, parallelTasksCount, action, getPerTaskArgument, progressReportAction, delayInMilliseconds));

    #region Helpers
    private static int GetIteratedIndex(int number, int count) => number % count;
    #endregion
}
