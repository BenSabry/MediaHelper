using Domain.DTO;
using Domain.Enums;

namespace Application.Infrastructure.Services;
public interface ILoggerService : IDisposable
{
    void Log(LogOperation operation, params string[] messages);
    void Log(LogLevel level, string message, bool appendLine = true);

    void LogCritical(string message, bool appendLine = true);
    void LogError(string message, bool appendLine = true);
    void LogInformation(string message, bool appendLine = true);
    void LogSuccess(string message, bool appendLine = true);
    void LogWarning(string message, bool appendLine = true);

    void ClearShell();
    void WaitUserInteraction();
    LogRecord[] ReadAllLogs();
}
