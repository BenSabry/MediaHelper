namespace Shared.Common.Abstracts;

public abstract class AbstractThrottler : IDisposable
{
    protected Action action { get; init; }
    protected TimeSpan delay { get; init; }
    protected CancellationTokenSource CS { get; set; } = new();
    protected bool isWorking { get; set; }
    protected DateTime lastRegister { get; set; }

    protected AbstractThrottler(Action action, int millisecondsDelay)
    {
        this.action = action;
        delay = new TimeSpan(0, 0, 0, 0, millisecondsDelay);
    }
    public abstract void RegisterAsync();

    #region Dispose
    private bool disposed;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed) return;
        if (disposing)
        {
            CS.Cancel();
            CS.Dispose();
        }

        disposed = true;
    }
    #endregion
}

public abstract class AbstractThrottler<T> : IDisposable
{
    protected Action<T> action { get; init; }
    protected TimeSpan delay { get; init; }
    protected CancellationTokenSource CS { get; set; } = new();
    protected bool isWorking { get; set; }
    protected DateTime lastRegister { get; set; }

    protected AbstractThrottler(Action<T> action, int millisecondsDelay)
    {
        this.action = action;
        delay = new TimeSpan(0, 0, 0, 0, millisecondsDelay);
    }
    public abstract void RegisterAsync(T arg);

    #region Dispose
    private bool disposed;
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed) return;
        if (disposing)
        {
            CS.Cancel();
            CS.Dispose();
        }

        disposed = true;
    }
    #endregion
}
