using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackUnderflow.Migrations
{
    /// <inheritdoc />
    public partial class AddModerators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsModerator",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsModerator",
                table: "AspNetUsers");
        }
    }
}
