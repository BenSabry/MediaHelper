using Shared.Common.Abstracts;

namespace Shared;

public sealed class Throttler : AbstractThrottler
{
    public Throttler(Action action, int millisecondsDelay) : base(action, millisecondsDelay) { }
    public override void RegisterAsync()
    {
        if (isWorking) return;

        isWorking = true;
        Task.Run(async () =>
        {
            CS.Token.ThrowIfCancellationRequested();
            action();

            if (!CS.Token.IsCancellationRequested)
                await Task.Delay(delay);

            isWorking = false;
        }, CS.Token);
    }
}

public sealed class Throttler<T> : AbstractThrottler<T>
{
    public Throttler(Action<T> action, int millisecondsDelay) : base(action, millisecondsDelay) { }
    public override void RegisterAsync(T argument)
    {
        if (isWorking) return;

        isWorking = true;
        var arg = argument;
        Task.Run(async () =>
        {
            CS.Token.ThrowIfCancellationRequested();
            action(arg);

            if (!CS.Token.IsCancellationRequested)
                await Task.Delay(delay);

            isWorking = false;
        }, CS.Token);
    }
}
