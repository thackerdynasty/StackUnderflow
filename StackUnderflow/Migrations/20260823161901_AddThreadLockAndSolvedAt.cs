using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackUnderflow.Migrations
{
    /// <inheritdoc />
    public partial class AddThreadLockAndSolvedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "SUThreads",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SolvedAt",
                table: "SUThreads",
                type: "datetime2",
                nullable: true);

            // Backfill a solved timestamp for threads that were already solved before
            // this column existed, so the auto-lock sweep can evaluate them. UpdatedAt is
            // the best available proxy for when the thread was last resolved.
            migrationBuilder.Sql(
                "UPDATE SUThreads SET SolvedAt = UpdatedAt WHERE IsSolved = 1 AND SolvedAt IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "SUThreads");

            migrationBuilder.DropColumn(
                name: "SolvedAt",
                table: "SUThreads");
        }
    }
}
