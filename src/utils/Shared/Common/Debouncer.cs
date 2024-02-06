using Shared.Common.Abstracts;

namespace Shared;

public sealed class Debouncer : AbstractThrottler
{
    public Debouncer(Action action, int millisecondsDelay) : base(action, millisecondsDelay) { }

    public override void RegisterAsync()
    {
        lastRegister = DateTime.Now;
        if (isWorking) return;

        isWorking = true;
        Task.Run(async () =>
        {
            while (!CS.Token.IsCancellationRequested)
            {
                var remaining = DateTime.Now - lastRegister;
                if (remaining.TotalMilliseconds > 0)
                    await Task.Delay(remaining);
                else break;
            }

            if (!CS.Token.IsCancellationRequested) action();
            isWorking = false;
        });
    }
}

public sealed class Debouncer<T> : AbstractThrottler<T>
{
    public Debouncer(Action<T> action, int millisecondsDelay) : base(action, millisecondsDelay) { }

    public override void RegisterAsync(T argument)
    {
        lastRegister = DateTime.Now;
        if (isWorking) return;

        isWorking = true;
        var arg = argument;
        Task.Run(async () =>
        {
            while (!CS.Token.IsCancellationRequested)
            {
                var remaining = DateTime.Now - lastRegister;
                if (remaining.TotalMilliseconds > 0)
                    await Task.Delay(remaining);
                else break;
            }

            if (!CS.Token.IsCancellationRequested) action(arg);
            isWorking = false;
        });
    }
}
