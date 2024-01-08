using System.Collections.Concurrent;
using System.Text;

namespace MediaOrganizer.Helpers;
public class LogHelper : IDisposable
{
    #region Fields-Static
    private static readonly string TempDirectory = Path.Combine(CommonHelper.TempDirectory, "Log");
    private static readonly ConsoleColor DefaultConsoleColor = ConsoleColor.Gray;

    private static readonly string GlobalLogFilePath;
    private static readonly Guid GlobalId = Guid.NewGuid();
    private static readonly object GlobalLock = new();

    private static Dictionary<Guid, ConcurrentQueue<(DateTime Time, string Value)>> DB = new();
    private const string LogSeparator = ";";
    #endregion

    #region Fields-Instance
    private readonly Guid Id = Guid.NewGuid();
    private readonly bool Enabled;
    #endregion

    #region Constructors
    static LogHelper()
    {
        var date = DateTime.Now;
        const int length = 2;
        GlobalLogFilePath = @$"{TempDirectory}\{date.Year}"
            + $"{CommonHelper.FormatNumberToLength(date.Month, length)}"
            + $"{CommonHelper.FormatNumberToLength(date.Day, length)}"
            + $"{CommonHelper.FormatNumberToLength(date.Hour, length)}"
            + $"{CommonHelper.FormatNumberToLength(date.Minute, length)}"
            + $".{GlobalId.ToString().Replace("-", string.Empty)}.txt";

        lock (GlobalLock)
        {
            if (!Directory.Exists(TempDirectory))
                Directory.CreateDirectory(TempDirectory);

            if (File.Exists(GlobalLogFilePath))
                File.Delete(GlobalLogFilePath);

            File.CreateText(GlobalLogFilePath).Close();
        }
    }
    public LogHelper(bool enabled = true)
    {
        Enabled = enabled;
        if (Enabled)
            DB.Add(Id, new ConcurrentQueue<(DateTime Time, string Value)>());
    }
    #endregion

    #region Destructor
    ~LogHelper()
    {
        Dispose();
    }
    #endregion

    #region Behavior-Static
    private static void Log(string message, ConsoleColor color, bool addNewLine = true)
    {
        Console.ForegroundColor = color;

        if (addNewLine) Console.WriteLine(message);
        else Console.Write(message);

        Console.ForegroundColor = DefaultConsoleColor;
    }
    public static void Notice(string message, bool addNewLine = true)
    {
        Log(message, ConsoleColor.Blue, addNewLine);
    }
    public static void Message(string message, bool addNewLine = true)
    {
        Log(message, DefaultConsoleColor, addNewLine);
    }
    public static void Warning(string message, bool addNewLine = true)
    {
        Log(message, ConsoleColor.DarkYellow, addNewLine);
    }
    public static void Success(string message, bool addNewLine = true)
    {
        Log(message, ConsoleColor.DarkGreen, addNewLine);
    }
    public static void Error(string message, bool addNewLine = true)
    {
        Log(message, ConsoleColor.Red, addNewLine);
    }

    public static void Clear()
    {
        Console.Clear();
    }
    public static char ReadKey()
    {
        return Console.ReadKey().KeyChar;
    }
    public static void SaveLog()
    {
        lock (GlobalLock)
        {
            var logs = new List<(DateTime Time, string Value)>();
            foreach (var queue in DB)
                while (queue.Value.TryDequeue(out var item))
                    logs.Add(item);

            if (logs.Any())
            {
                var sb = new StringBuilder();
                foreach (var item in logs.OrderBy(i => i.Time))
                    sb.AppendLine($"{item.Time};{item.Value}");

                File.AppendAllText(GlobalLogFilePath, sb.ToString());
            }
        }
    }
    public static LogRecord[] ReadAllLogs()
    {
        if (!Directory.Exists(TempDirectory))
            return Array.Empty<LogRecord>();

        var list = new List<LogRecord>();
        foreach (var file in Directory.EnumerateFiles(TempDirectory))
            foreach (var line in File.ReadAllLines(file))
            {
                var parts = line.Split(LogSeparator);
                list.Add(new LogRecord(
                    DateTime.Parse(parts[0]),
                    long.Parse(parts[1]),
                    GetOperation(parts[2]),
                    parts[3],
                    parts.Length > 4 ? parts[4] : string.Empty));
            }

        return list.ToArray();
    }

    private static LogOperation GetOperation(string operationName)
    {
        if (string.Equals(operationName, "Update", StringComparison.OrdinalIgnoreCase)) return LogOperation.Update;
        if (string.Equals(operationName, "Copy", StringComparison.OrdinalIgnoreCase)) return LogOperation.Copy;

        return LogOperation.Fail;
    }
    #endregion

    #region Behavior-Instance
    public void Log(LogOperation operation, params string[] parts)
    {
        if (Enabled)
            DB[Id].Enqueue((DateTime.Now, $"{operation};{string.Join(LogSeparator, parts)}"));
    }
    public void Dispose()
    {
        SaveLog();
    }
    #endregion
}

public record struct LogRecord(DateTime DateTime, long Index, LogOperation Operation, string Source, string Destination);
public enum LogOperation
{
    Update,
    Copy,
    Fail
}