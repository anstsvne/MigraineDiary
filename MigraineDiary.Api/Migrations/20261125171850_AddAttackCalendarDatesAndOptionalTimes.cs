using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigraineDiary.Api.Migrations
{
    public partial class AddAttackCalendarDatesAndOptionalTimes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MigraineAttacks_UserId",
                table: "MigraineAttacks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MigraineAttacks_EndAt",
                table: "MigraineAttacks");

            // Add calendar dates as temporarily nullable columns.
            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "MigraineAttacks",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "MigraineAttacks",
                type: "date",
                nullable: true);

            // Preserve original UTC instants and derive UTC calendar dates.
            migrationBuilder.Sql(
                """
                UPDATE "MigraineAttacks"
                SET
                    "StartDate" = ("StartAt" AT TIME ZONE 'UTC')::date,
                    "EndDate" = ("EndAt" AT TIME ZONE 'UTC')::date;
                """);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "StartDate",
                table: "MigraineAttacks",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "EndDate",
                table: "MigraineAttacks",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartAt",
                table: "MigraineAttacks",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "EndAt",
                table: "MigraineAttacks",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateIndex(
                name: "IX_MigraineAttacks_UserId_EndDate",
                table: "MigraineAttacks",
                columns: new[] { "UserId", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MigraineAttacks_UserId_StartDate",
                table: "MigraineAttacks",
                columns: new[] { "UserId", "StartDate" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_MigraineAttacks_DateRange",
                table: "MigraineAttacks",
                sql: "\"EndDate\" >= \"StartDate\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MigraineAttacks_ExactTimeRange",
                table: "MigraineAttacks",
                sql: "\"StartAt\" IS NULL OR \"EndAt\" IS NULL OR \"EndAt\" >= \"StartAt\"");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Do not fabricate exact times when reverting a database that contains
            // calendar-only attacks. Stop the downgrade before changing the schema.
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "MigraineAttacks"
                        WHERE "StartAt" IS NULL
                           OR "EndAt" IS NULL
                    ) THEN
                        RAISE EXCEPTION
                            'Cannot downgrade: some migraine attacks have no exact start or end time.';
                    END IF;
                END $$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_MigraineAttacks_UserId_EndDate",
                table: "MigraineAttacks");

            migrationBuilder.DropIndex(
                name: "IX_MigraineAttacks_UserId_StartDate",
                table: "MigraineAttacks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MigraineAttacks_DateRange",
                table: "MigraineAttacks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_MigraineAttacks_ExactTimeRange",
                table: "MigraineAttacks");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "StartAt",
                table: "MigraineAttacks",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "EndAt",
                table: "MigraineAttacks",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "MigraineAttacks");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "MigraineAttacks");

            migrationBuilder.CreateIndex(
                name: "IX_MigraineAttacks_UserId",
                table: "MigraineAttacks",
                column: "UserId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MigraineAttacks_EndAt",
                table: "MigraineAttacks",
                sql: "\"EndAt\" >= \"StartAt\"");
        }
    }
}
