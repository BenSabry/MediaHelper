using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using Shared;


Benchmarker benchmarker;
Console.WriteLine("Running...");

benchmarker = new Benchmarker();
benchmarker.Throttler();
Console.WriteLine(benchmarker.Count);

//benchmarker = new Benchmarker();
//benchmarker.ThreadSafeThrottler();
//Console.WriteLine(benchmarker.Count);

Task.Delay(10).Wait();

var summary = BenchmarkRunner.Run<Benchmarker>();

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.NativeAot80)]
public class Benchmarker
{
    #region Fields
    private const int Delay = 100;
    private const int Repeats = 500;
    private const int RepeatDelay = 1;
    private const int TasksCount = 1000;

    private int[] Counters;
    public int Count => Counters.Sum();
    #endregion

    #region Defaults
    private bool Initialized;

    public Benchmarker() => Setup();

    [GlobalSetup]
    public void Setup()
    {
        if (Initialized) return;
        Initialized = true;

        Initialize();
    }
    #endregion

    private void Initialize()
    {
        Counters = new int[TasksCount];
    }

    [Benchmark]
    public void Throttler()
    {
        Initialize();
        using (var worker = new Throttler<int>(DoSomeWork, Delay))
            Repeat(worker.RegisterAsync);
    }

    //[Benchmark]
    //public void ThreadSafeThrottler()
    //{
    //    Initialize();
    //    using (var worker = new ThreadSafeThrottler<int>(DoSomeWork, Delay))
    //        Repeat(worker.RegisterAsync);
    //}

    private static void Repeat(Action<int> action)
    {
        var tasks = new Task[TasksCount];
        for (int tIndex = 0; tIndex < TasksCount; tIndex++)
        {
            var index = tIndex;
            tasks[index] = Task.Run(() =>
            {
                for (int i = 0; i < Repeats; i++)
                {
                    action(index);
                    Task.Delay(RepeatDelay).Wait();
                }
            });
        }

        Task.WaitAll(tasks);
    }
    private void DoSomeWork(int index)
    {
        Counters[index]++;
    }
}

//public class DebouncerThreaded : AbstractBounce
//{
//    public DebouncerThreaded(Action action, int millisecondsDelay) : base(action, millisecondsDelay) { }
//    public override void RegisterAsync()
//    {
//        CS.Cancel();
//        CS = RegisterDebounce();
//    }

//    private CancellationTokenSource RegisterDebounce()
//    {
//        var cs = new CancellationTokenSource();
//        Task.Run(async () =>
//        {
//            cs.Token.ThrowIfCancellationRequested();
//            await Task.Delay(delay);

//            if (!cs.Token.IsCancellationRequested) action();

//        }, cs.Token);

//        return cs;
//    }
//}


