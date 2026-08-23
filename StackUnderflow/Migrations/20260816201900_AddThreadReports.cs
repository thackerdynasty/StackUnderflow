using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackUnderflow.Migrations
{
    /// <inheritdoc />
    public partial class AddThreadReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThreadReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModeratorNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReporterId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReviewedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SUThreadId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThreadReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThreadReports_AspNetUsers_ReporterId",
                        column: x => x.ReporterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ThreadReports_AspNetUsers_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ThreadReports_SUThreads_SUThreadId",
                        column: x => x.SUThreadId,
                        principalTable: "SUThreads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThreadReports_ReporterId_SUThreadId",
                table: "ThreadReports",
                columns: new[] { "ReporterId", "SUThreadId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThreadReports_ReviewedById",
                table: "ThreadReports",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_ThreadReports_SUThreadId",
                table: "ThreadReports",
                column: "SUThreadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThreadReports");
        }
    }
}
