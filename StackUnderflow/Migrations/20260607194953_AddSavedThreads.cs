using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackUnderflow.Migrations
{
    /// <inheritdoc />
    public partial class AddSavedThreads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SavedThreads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SavedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SUThreadId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedThreads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedThreads_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SavedThreads_SUThreads_SUThreadId",
                        column: x => x.SUThreadId,
                        principalTable: "SUThreads",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SavedThreads_SUThreadId",
                table: "SavedThreads",
                column: "SUThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedThreads_UserId_SUThreadId",
                table: "SavedThreads",
                columns: new[] { "UserId", "SUThreadId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SavedThreads");
        }
    }
}
