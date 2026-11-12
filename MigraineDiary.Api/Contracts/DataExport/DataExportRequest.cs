namespace MigraineDiary.Api.Contracts.DataExport;

public sealed class DataExportRequest
{
    public DateTimeOffset? From { get; init; }

    public DateTimeOffset? To { get; init; }
}