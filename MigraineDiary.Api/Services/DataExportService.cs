
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
            "CreatedAtUtc");

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
                    .ToString("O"));
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
            "CreatedAtUtc");

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
                    .ToString("O"));
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

        if (details.Count == 0)
        {
            return medication.Medication.Name;
        }

        return $"{medication.Medication.Name} [{string.Join(", ", details)}]";
    }

    private static void AppendCsvRow(
        StringBuilder builder,
        params string?[] values)
    {
        builder.AppendLine(
            string.Join(
                ";",
                values.Select(EscapeCsvValue)));
    }

    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        var normalized = value
            .Replace("\r\n", " ")
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace("\"", "\"\"");

        return $"\"{normalized}\"";
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
        if (request.From.HasValue &&
            request.To.HasValue &&
            request.To.Value < request.From.Value)
        {
            throw new ArgumentException(
                "Конец периода не может быть раньше начала периода.");
        }
    }
}
