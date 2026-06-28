using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackUnderflow.Migrations
{
    public partial class AddSUThreadColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safer: add nullable SUThreadId to Posts so migration won't fail on existing rows.
            migrationBuilder.AddColumn<int>(
                name: "SUThreadId",
                table: "Posts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_SUThreadId",
                table: "Posts",
                column: "SUThreadId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_SUThreads_SUThreadId",
                table: "Posts",
                column: "SUThreadId",
                principalTable: "SUThreads",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            // Safer: add nullable SUThreadId to ThreadVotes.
            migrationBuilder.AddColumn<int>(
                name: "SUThreadId",
                table: "ThreadVotes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThreadVotes_SUThreadId",
                table: "ThreadVotes",
                column: "SUThreadId");

            migrationBuilder.AddForeignKey(
                name: "FK_ThreadVotes_SUThreads_SUThreadId",
                table: "ThreadVotes",
                column: "SUThreadId",
                principalTable: "SUThreads",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_SUThreads_SUThreadId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_SUThreadId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "SUThreadId",
                table: "Posts");

            migrationBuilder.DropForeignKey(
                name: "FK_ThreadVotes_SUThreads_SUThreadId",
                table: "ThreadVotes");

            migrationBuilder.DropIndex(
                name: "IX_ThreadVotes_SUThreadId",
                table: "ThreadVotes");

            migrationBuilder.DropColumn(
                name: "SUThreadId",
                table: "ThreadVotes");
        }
    }
}
