using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigraineDiary.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVitalMeasurementFieldsAndAttackLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BodyPosition",
                table: "VitalMeasurements",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "MigraineAttackId",
                table: "VitalMeasurements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VitalMeasurements_MigraineAttackId",
                table: "VitalMeasurements",
                column: "MigraineAttackId");

            migrationBuilder.AddForeignKey(
                name: "FK_VitalMeasurements_MigraineAttacks_MigraineAttackId",
                table: "VitalMeasurements",
                column: "MigraineAttackId",
                principalTable: "MigraineAttacks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VitalMeasurements_MigraineAttacks_MigraineAttackId",
                table: "VitalMeasurements");

            migrationBuilder.DropIndex(
                name: "IX_VitalMeasurements_MigraineAttackId",
                table: "VitalMeasurements");

            migrationBuilder.DropColumn(
                name: "BodyPosition",
                table: "VitalMeasurements");

            migrationBuilder.DropColumn(
                name: "MigraineAttackId",
                table: "VitalMeasurements");
        }
    }
}
