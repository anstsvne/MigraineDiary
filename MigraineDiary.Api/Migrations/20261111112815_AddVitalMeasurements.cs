using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigraineDiary.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVitalMeasurements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VitalMeasurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SystolicPressure = table.Column<int>(type: "integer", nullable: false),
                    DiastolicPressure = table.Column<int>(type: "integer", nullable: false),
                    Pulse = table.Column<int>(type: "integer", nullable: false),
                    MeasuredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VitalMeasurements", x => x.Id);
                    table.CheckConstraint("CK_VitalMeasurements_DiastolicPressure", "\"DiastolicPressure\" >= 1 AND \"DiastolicPressure\" <= 200");
                    table.CheckConstraint("CK_VitalMeasurements_Pulse", "\"Pulse\" >= 1 AND \"Pulse\" <= 300");
                    table.CheckConstraint("CK_VitalMeasurements_SystolicPressure", "\"SystolicPressure\" >= 1 AND \"SystolicPressure\" <= 300");
                    table.ForeignKey(
                        name: "FK_VitalMeasurements_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VitalMeasurements_UserId_MeasuredAt",
                table: "VitalMeasurements",
                columns: new[] { "UserId", "MeasuredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VitalMeasurements");
        }
    }
}
