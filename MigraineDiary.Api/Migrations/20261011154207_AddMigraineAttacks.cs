using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigraineDiary.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMigraineAttacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MigraineAttacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Intensity = table.Column<int>(type: "integer", nullable: false),
                    PainLocation = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PainLocationDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MigraineAttacks", x => x.Id);
                    table.CheckConstraint("CK_MigraineAttacks_EndAt", "\"EndAt\" >= \"StartAt\"");
                    table.CheckConstraint("CK_MigraineAttacks_Intensity", "\"Intensity\" >= 1 AND \"Intensity\" <= 10");
                    table.ForeignKey(
                        name: "FK_MigraineAttacks_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MigraineAttacks_UserId",
                table: "MigraineAttacks",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MigraineAttacks");
        }
    }
}
