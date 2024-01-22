using System.Collections.Concurrent;
using Application.Infrastructure.Services;
using Domain.DTO;
using Domain.Enums;
using Domain.Interfaces;
using Shared.Helpers;
using Shared.Wrappers;

namespace Infrastructure.Services;
public sealed class LoggerService : ILoggerService
{
    //TODO: use properate standard logger
    #region Fields
    private const string LogSeparator = ";";
    private readonly string RootPath;
    private readonly string CurrentFilePath;

    private readonly object LOCK = new();
    private readonly ConcurrentQueue<(DateTime Time, string Value)> Queue = new();

    private readonly bool Enabled;
    #endregion

    #region Constructors
    public LoggerService(ISettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Enabled = settings.EnableLogAndResume;
        RootPath = settings.TempLogPath;

        var date = DateTime.Now;
        const int length = 2;

        CurrentFilePath = @$"{RootPath}\{date.Year}"
            + $"{CommonHelper.FormatNumberToLength(date.Month, length)}"
            + $"{CommonHelper.FormatNumberToLength(date.Day, length)}"
            + $"{CommonHelper.FormatNumberToLength(date.Hour, length)}"
            + $"{CommonHelper.FormatNumberToLength(date.Minute, length)}"
            + $"{CommonHelper.FormatNumberToLength(date.Second, length)}"
            + $".csv";

        if (Enabled)
        {
            if (!Directory.Exists(RootPath))
                Directory.CreateDirectory(RootPath);

            if (File.Exists(CurrentFilePath))
                File.Delete(CurrentFilePath);

            File.CreateText(CurrentFilePath).Close();
        }

        StartSaveTask(settings.LogSaveDelay);
    }
    #endregion

    #region Destructor
    ~LoggerService()
    {
        Dispose(false);
    }
    #endregion

    #region Behavior-Instance
    public void Log(LogOperation operation, params string[] messages)
    {
        if (Enabled)
            Queue.Enqueue((DateTime.Now, $"{operation};{string.Join(LogSeparator, messages)}"));
    }
    public void Log(LogLevel level, string message, bool appendLine = true)
    {
        var color = ConsoleColor.Gray;
        switch (level)
        {
            case LogLevel.Critical: color = ConsoleColor.Blue; break;
            case LogLevel.Error: color = ConsoleColor.Red; break;
            case LogLevel.Success: color = ConsoleColor.DarkGreen; break;
            case LogLevel.Warning: color = ConsoleColor.DarkYellow; break;
        }

        Console.ForegroundColor = color;

        if (appendLine)
            Console.WriteLine(message);
        else Console.Write(message);
    }

    public void LogCritical(string message, bool appendLine = true) => Log(LogLevel.Critical, message, appendLine);
    public void LogError(string message, bool appendLine = true) => Log(LogLevel.Error, message, appendLine);
    public void LogInformation(string message, bool appendLine = true) => Log(LogLevel.Information, message, appendLine);
    public void LogSuccess(string message, bool appendLine = true) => Log(LogLevel.Success, message, appendLine);
    public void LogWarning(string message, bool appendLine = true) => Log(LogLevel.Warning, message, appendLine);

    public void ClearShell()
    {
        Console.Clear();
    }
    public void WaitUserInteraction()
    {
        Console.ReadKey();
    }
    public LogRecord[] ReadAllLogs()
    {
        if (!Directory.Exists(RootPath))
            return Array.Empty<LogRecord>();

        var list = new List<LogRecord>();
        foreach (var file in Directory.EnumerateFiles(RootPath))
            foreach (var line in File.ReadAllLines(file))
            {
                var parts = line.Split(LogSeparator);
                list.Add(new LogRecord(
                    DateTime.Parse(parts[0]),
                    Enum<LogOperation>.GetByName(parts[1]),
                    parts[2],
                    parts.Length > 3 ? parts[3] : string.Empty));
            }

        return list.ToArray();
    }

    private void StartSaveTask(int delayInMillisecods)
    {
        Task.Run(async () =>
        {
            while (Enabled)
            {
                await Task.Delay(delayInMillisecods);
                SaveLog();
            }
        });
    }
    private void SaveLog()
    {
        lock (LOCK)
            if (!Queue.IsEmpty)
                File.AppendAllLines(CurrentFilePath, Queue.Select(i => $"{i.Time}{LogSeparator}{i.Value}"));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            SaveLog();
        }
    }
    #endregion
}
