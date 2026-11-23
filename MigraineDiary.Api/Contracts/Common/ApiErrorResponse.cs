using System.Text.Json.Serialization;

namespace MigraineDiary.Api.Contracts.Common;

public sealed class ApiErrorResponse
{
    public int Status { get; init; }

    public required string Code { get; init; }

    public required string Message { get; init; }

    [JsonIgnore(
        Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    public required string TraceId { get; init; }
}