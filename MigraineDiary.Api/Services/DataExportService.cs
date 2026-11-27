
using System.Globalization;
using System.IO.Compression;
using System.Text;
using MigraineDiary.Api.Contracts.DataExport;
using MigraineDiary.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace MigraineDiary.Api.Services;

public sealed class DataExportService
{
    private readonly AppDbContext _dbContext;

    public DataExportService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<byte[]> ExportAsync(
        Guid userId,
        DataExportRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request);

        var attacksQuery = _dbContext.MigraineAttacks
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        var vitalMeasurementsQuery = _dbContext.VitalMeasurements
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (request.From.HasValue)
        {
            var fromUtc = request.From.Value.ToUniversalTime();

            // Для приступов используем календарную дату
            // с учетом исходного смещения запроса.
            var fromDate = DateOnly.FromDateTime(
                request.From.Value.DateTime);

            attacksQuery = attacksQuery.Where(x =>
                x.EndDate >= fromDate);

            // Измерения продолжаем фильтровать по точному времени.
            vitalMeasurementsQuery =
                vitalMeasurementsQuery.Where(x =>
                    x.MeasuredAt >= fromUtc);
        }

        if (request.To.HasValue)
        {
            var toUtc = request.To.Value.ToUniversalTime();

            var toDate = DateOnly.FromDateTime(
                request.To.Value.DateTime);

            attacksQuery = attacksQuery.Where(x =>
                x.StartDate <= toDate);

            vitalMeasurementsQuery =
                vitalMeasurementsQuery.Where(x =>
                    x.MeasuredAt <= toUtc);
        }

        if (request.FromDate is DateOnly lowerDate)
        {
            attacksQuery = attacksQuery.Where(x => x.EndDate >= lowerDate);
            var fromUtc = new DateTimeOffset(lowerDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            vitalMeasurementsQuery = vitalMeasurementsQuery.Where(x => x.MeasuredAt >= fromUtc);
        }

        if (request.ToDate is DateOnly upperDate)
        {
            attacksQuery = attacksQuery.Where(x => x.StartDate <= upperDate);
            if (upperDate != DateOnly.MaxValue)
            {
                var toExclusive = new DateTimeOffset(upperDate.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                vitalMeasurementsQuery = vitalMeasurementsQuery.Where(x => x.MeasuredAt < toExclusive);
            }
        }

        var attacks = await attacksQuery
            .AsSplitQuery()
            .Include(x => x.Symptoms)
                .ThenInclude(x => x.Symptom)
            .Include(x => x.Triggers)
                .ThenInclude(x => x.Trigger)
            .Include(x => x.Medications)
                .ThenInclude(x => x.Medication)
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.StartAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var vitalMeasurements = await vitalMeasurementsQuery
            .OrderBy(x => x.MeasuredAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var attacksCsv = BuildAttacksCsv(attacks);

        var vitalMeasurementsCsv =
            BuildVitalMeasurementsCsv(vitalMeasurements);

        await using var archiveStream = new MemoryStream();

        using (var archive = new ZipArchive(
                   archiveStream,
                   ZipArchiveMode.Create,
                   leaveOpen: true))
        {
            await AddCsvEntryAsync(
                archive,
                "migraine-attacks.csv",
                attacksCsv,
                cancellationToken);

            await AddCsvEntryAsync(
                archive,
                "vital-measurements.csv",
                vitalMeasurementsCsv,
                cancellationToken);
        }

        return archiveStream.ToArray();
    }

    private static string BuildAttacksCsv(
        IReadOnlyCollection<Entities.MigraineAttack> attacks)
    {
        var builder = new StringBuilder();

        AppendCsvRow(
            builder,
            "Id",
            "StartDate",
            "EndDate",
            "StartAtUtc",
            "EndAtUtc",
            "DurationMinutes",
            "Intensity",
            "PainLocation",
            "PainLocationDescription",
            "Notes",
            "Symptoms",
            "Triggers",
            "Medications",
            "CreatedAtUtc",
            "DurationDays");

        foreach (var attack in attacks)
        {
            var symptoms = string.Join(
                " | ",
                attack.Symptoms
                    .OrderBy(x => x.Symptom.Name)
                    .Select(x =>
                        BuildNamedItem(
                            x.Symptom.Name,
                            x.Description)));

            var triggers = string.Join(
                " | ",
                attack.Triggers
                    .OrderBy(x => x.Trigger.Name)
                    .Select(x =>
                        BuildNamedItem(
                            x.Trigger.Name,
                            x.Description)));

            var medications = string.Join(
                " | ",
                attack.Medications
                    .OrderBy(x => x.TakenAt)
                    .ThenBy(x => x.Medication.Name)
                    .ThenBy(x => x.Id)
                    .Select(BuildMedicationItem));

            double? durationMinutes = null;

            if (attack.StartAt.HasValue &&
                attack.EndAt.HasValue)
            {
                durationMinutes = (
                    attack.EndAt.Value -
                    attack.StartAt.Value
                ).TotalMinutes;
            }

            AppendCsvRow(
                builder,
                attack.Id.ToString(),

                attack.StartDate.ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture),

                attack.EndDate.ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture),

                attack.StartAt?.ToUniversalTime()
                    .ToString("O"),

                attack.EndAt?.ToUniversalTime()
                    .ToString("O"),

                durationMinutes?.ToString(
                    "0.##",
                    CultureInfo.InvariantCulture),

                attack.Intensity.ToString(
                    CultureInfo.InvariantCulture),

                attack.PainLocation.ToString(),
                attack.PainLocationDescription,
                attack.Notes,
                symptoms,
                triggers,
                medications,

                attack.CreatedAt
                    .ToUniversalTime()
                    .ToString("O"),

                (attack.EndDate.DayNumber - attack.StartDate.DayNumber + 1)
                    .ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static string BuildVitalMeasurementsCsv(
        IReadOnlyCollection<Entities.VitalMeasurement> measurements)
    {
        var builder = new StringBuilder();

        AppendCsvRow(
            builder,
            "Id",
            "SystolicPressure",
            "DiastolicPressure",
            "Pulse",
            "MeasuredAtUtc",
            "Notes",
            "CreatedAtUtc",
            "MigraineAttackId",
            "BodyPosition");

        foreach (var measurement in measurements)
        {
            AppendCsvRow(
                builder,
                measurement.Id.ToString(),

                measurement.SystolicPressure.ToString(
                    CultureInfo.InvariantCulture),

                measurement.DiastolicPressure.ToString(
                    CultureInfo.InvariantCulture),

                measurement.Pulse.ToString(
                    CultureInfo.InvariantCulture),

                measurement.MeasuredAt
                    .ToUniversalTime()
                    .ToString("O"),

                measurement.Notes,

                measurement.CreatedAt
                    .ToUniversalTime()
                    .ToString("O"),

                measurement.MigraineAttackId?.ToString(),
                measurement.BodyPosition?.ToString());
        }

        return builder.ToString();
    }

    private static string BuildNamedItem(
        string name,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return name;
        }

        return $"{name} ({description})";
    }

    private static string BuildMedicationItem(
        Entities.AttackMedication medication)
    {
        var details = new List<string>();

        if (!string.IsNullOrWhiteSpace(medication.Dosage))
        {
            details.Add(medication.Dosage);
        }

        if (medication.TakenAt.HasValue)
        {
            details.Add(
                medication.TakenAt.Value
                    .ToUniversalTime()
                    .ToString("O"));
        }

        // Record effectiveness for every intake, even if it was not assessed.
        details.Add($"Effectiveness={medication.Effectiveness}");

        return $"{medication.Medication.Name} [{string.Join(", ", details)}]";
    }

    private static void AppendCsvRow(
        StringBuilder builder,
        params string?[] values)
    {
        builder.Append(string.Join(";", values.Select(EscapeCsvValue)));
        builder.Append("\r\n");
    }

    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        // Keep the existing single-line cell format and protect spreadsheet
        // applications from treating user-provided text as a formula.
        var normalized = value
            .Replace("\r\n", " ")
            .Replace('\r', ' ')
            .Replace('\n', ' ');

        var trimmed = normalized.TrimStart();
        if ((value[0] is '\t' or '\r' or '\n') ||
            (trimmed.Length > 0 && (trimmed[0] is '=' or '+' or '-' or '@'
                or '\uFF1D' or '\uFF0B' or '\uFF0D' or '\uFF20')))
        {
            normalized = "'" + normalized;
        }

        return $"\"{normalized.Replace("\"", "\"\"")}\"";
    }

    private static async Task AddCsvEntryAsync(
        ZipArchive archive,
        string fileName,
        string content,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(
            fileName,
            CompressionLevel.Fastest);

        await using var stream = entry.Open();

        await using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: true),
            1024,
            leaveOpen: false);

        await writer.WriteAsync(
            content.AsMemory(),
            cancellationToken);
    }

    private static void ValidatePeriod(
        DataExportRequest request)
    {
        if ((request.From.HasValue || request.To.HasValue) &&
            (request.FromDate.HasValue || request.ToDate.HasValue))
        {
            throw new ArgumentException("Timestamp and calendar date filters cannot be combined.");
        }

        if ((request.From.HasValue && request.To.HasValue && request.To.Value < request.From.Value) ||
            (request.FromDate.HasValue && request.ToDate.HasValue && request.ToDate.Value < request.FromDate.Value))
        {
            throw new ArgumentException(
                "Конец периода не может быть раньше начала периода.");
        }
    }
}
