using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MigraineDiary.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAttackSymptoms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Symptoms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Symptoms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttackSymptoms",
                columns: table => new
                {
                    MigraineAttackId = table.Column<Guid>(type: "uuid", nullable: false),
                    SymptomId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttackSymptoms", x => new { x.MigraineAttackId, x.SymptomId });
                    table.ForeignKey(
                        name: "FK_AttackSymptoms_MigraineAttacks_MigraineAttackId",
                        column: x => x.MigraineAttackId,
                        principalTable: "MigraineAttacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttackSymptoms_Symptoms_SymptomId",
                        column: x => x.SymptomId,
                        principalTable: "Symptoms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Symptoms",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Nausea" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "LightSensitivity" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "SoundSensitivity" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), "Dizziness" },
                    { new Guid("55555555-5555-5555-5555-555555555555"), "VisualDisturbance" },
                    { new Guid("66666666-6666-6666-6666-666666666666"), "Weakness" },
                    { new Guid("77777777-7777-7777-7777-777777777777"), "Other" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttackSymptoms_SymptomId",
                table: "AttackSymptoms",
                column: "SymptomId");

            migrationBuilder.CreateIndex(
                name: "IX_Symptoms_Name",
                table: "Symptoms",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttackSymptoms");

            migrationBuilder.DropTable(
                name: "Symptoms");
        }
    }
}
