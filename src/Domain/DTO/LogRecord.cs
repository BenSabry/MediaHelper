using Domain.Enums;

namespace Domain.DTO;

public record struct LogRecord(DateTime DateTime, LogOperation Operation, string Source, string Destination);
