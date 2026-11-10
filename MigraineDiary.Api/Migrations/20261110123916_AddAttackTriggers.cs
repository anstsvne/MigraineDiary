using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MigraineDiary.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAttackTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Triggers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Triggers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AttackTriggers",
                columns: table => new
                {
                    MigraineAttackId = table.Column<Guid>(type: "uuid", nullable: false),
                    TriggerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttackTriggers", x => new { x.MigraineAttackId, x.TriggerId });
                    table.ForeignKey(
                        name: "FK_AttackTriggers_MigraineAttacks_MigraineAttackId",
                        column: x => x.MigraineAttackId,
                        principalTable: "MigraineAttacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttackTriggers_Triggers_TriggerId",
                        column: x => x.TriggerId,
                        principalTable: "Triggers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Triggers",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("81111111-1111-1111-1111-111111111111"), "Stress" },
                    { new Guid("82222222-2222-2222-2222-222222222222"), "LackOfSleep" },
                    { new Guid("83333333-3333-3333-3333-333333333333"), "WeatherChange" },
                    { new Guid("84444444-4444-4444-4444-444444444444"), "Hunger" },
                    { new Guid("85555555-5555-5555-5555-555555555555"), "CertainFoods" },
                    { new Guid("86666666-6666-6666-6666-666666666666"), "PhysicalExertion" },
                    { new Guid("87777777-7777-7777-7777-777777777777"), "BrightLight" },
                    { new Guid("88888888-8888-8888-8888-888888888888"), "LoudNoise" },
                    { new Guid("89999999-9999-9999-9999-999999999999"), "Other" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttackTriggers_TriggerId",
                table: "AttackTriggers",
                column: "TriggerId");

            migrationBuilder.CreateIndex(
                name: "IX_Triggers_Name",
                table: "Triggers",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttackTriggers");

            migrationBuilder.DropTable(
                name: "Triggers");
        }
    }
}
