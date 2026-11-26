using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigraineDiary.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixMigraineAttackExactTimeRangeConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MigraineAttacks_ExactTimeRange",
                table: "MigraineAttacks");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MigraineAttacks_ExactTimeRange",
                table: "MigraineAttacks",
                sql: "\"StartAt\" IS NULL OR \"EndAt\" IS NULL OR \"EndAt\" >= \"StartAt\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_MigraineAttacks_ExactTimeRange",
                table: "MigraineAttacks");

            migrationBuilder.AddCheckConstraint(
                name: "CK_MigraineAttacks_ExactTimeRange",
                table: "MigraineAttacks",
                sql: "((\"StartAt\" IS NULL AND \"EndAt\" IS NULL) OR (\"StartAt\" IS NOT NULL AND \"EndAt\" IS NOT NULL AND \"EndAt\" >= \"StartAt\"))");
        }
    }
}
