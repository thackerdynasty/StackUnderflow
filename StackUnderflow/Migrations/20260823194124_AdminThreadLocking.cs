using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackUnderflow.Migrations
{
    /// <inheritdoc />
    public partial class AdminThreadLocking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LockedByAdmin",
                table: "SUThreads",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockedByAdmin",
                table: "SUThreads");
        }
    }
}
