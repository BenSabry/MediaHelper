using System.Collections.Concurrent;

namespace Shared.Extensions;

public static class IEnumerableExtensions
{
    public static IEnumerable<T> SelectMany<T>(this IEnumerable<IEnumerable<T>> source) => source.SelectMany(a => a.Select(b => b));

    public static TOutput[] SelectParallel<TInput, TOutput>(this IList<TInput> source, Func<TInput, TOutput> function, int parallelTasksCount = 100)
    {
        if (source is null) return Array.Empty<TOutput>();

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

        var totalEnumurationCount = 0;
        var queues = new ConcurrentQueue<TInput>[parallelTasksCount].Fill();
        var enumerator = Task.Run(() =>
        {
            foreach (var item in source)
            {
                queues[GetIteratedIndex(totalEnumurationCount, parallelTasksCount)].Enqueue(item);
                Interlocked.Increment(ref totalEnumurationCount);
            }
        });

        var tasks = new Task[parallelTasksCount];
        var reporter = new Throttler<(long, long)>((arg) =>
        {
            if (progressReportAction is not null)
                progressReportAction(arg.Item1, arg.Item2);
        }, delayInMilliseconds);

        for (int taskIndex = 0; taskIndex < parallelTasksCount; taskIndex++)
        {
            var index = taskIndex;
            tasks[index] = Task.Run(() =>
            {
                var arg = getPerTaskArgument();
                while (!enumerator.IsCompleted || !queues[GetIteratedIndex(index, parallelTasksCount)].IsEmpty)
                {
                    while (queues[GetIteratedIndex(index, parallelTasksCount)].TryDequeue(out var item))
                    {
                        action(item, index, arg);
                        reporter.RegisterAsync((index, totalEnumurationCount));
                        index += parallelTasksCount;
                    }
                    Task.Delay(delayInMilliseconds).Wait();
                }
                arg.Dispose();
            });
        }

        enumerator.Wait();
        Task.WaitAll(tasks);
        reporter.Dispose();
    }

    public static Task ParallelForEachAsync<TInput, TArg>(this IEnumerable<TInput> source, int parallelTasksCount, Action<TInput, long, TArg> action, Func<TArg> getPerTaskArgument, Action<long, long>? progressReportAction = null, int delayInMilliseconds = 500)
    => Task.Run(() => ParallelForEach(source, parallelTasksCount, action, getPerTaskArgument, progressReportAction, delayInMilliseconds));

    public static void ParallelForEach<TInput>(this IEnumerable<TInput> source, int parallelTasksCount, Action<TInput, long> action, Action<long, long>? progressReportAction = null, int delayInMilliseconds = 500)
    => ParallelForEach(source, parallelTasksCount, (input, index, arg) => action(input, index), () => false, progressReportAction, delayInMilliseconds);

    public static Task ParallelForEachAsync<TInput>(this IEnumerable<TInput> source, int parallelTasksCount, Action<TInput, long> action, Action<long, long>? progressReportAction = null, int delayInMilliseconds = 500)
    => ParallelForEachAsync(source, parallelTasksCount, (input, index, arg) => action(input, index), () => false, progressReportAction, delayInMilliseconds);

    #region Helpers
    private static int GetIteratedIndex(int number, int count) => number % count;
    #endregion
}
