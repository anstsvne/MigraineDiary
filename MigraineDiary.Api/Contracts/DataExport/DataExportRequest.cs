namespace MigraineDiary.Api.Contracts.DataExport;

public sealed class DataExportRequest
{
    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }

    // Inclusive calendar dates; measurements use UTC dates.
    public DateOnly? FromDate { get; init; }

    public DateOnly? ToDate { get; init; }
}