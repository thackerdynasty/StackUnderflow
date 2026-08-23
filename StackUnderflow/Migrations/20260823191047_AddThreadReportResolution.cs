using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackUnderflow.Migrations
{
    /// <inheritdoc />
    public partial class AddThreadReportResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Resolution",
                table: "ThreadReports",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Resolution",
                table: "ThreadReports");
        }
    }
}
