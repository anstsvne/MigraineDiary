using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigraineDiary.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationEffectiveness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Effectiveness",
                table: "AttackMedications",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "NotAssessed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Effectiveness",
                table: "AttackMedications");
        }
    }
}
