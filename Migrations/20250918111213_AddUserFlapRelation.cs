using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flappr.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFlapRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImgUrl",
                table: "Flaps");

            migrationBuilder.DropColumn(
                name: "Nickname",
                table: "Flaps");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "Flaps");

            migrationBuilder.CreateIndex(
                name: "IX_Flaps_UserId",
                table: "Flaps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserId",
                table: "Comments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Users_UserId",
                table: "Comments",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Flaps_Users_UserId",
                table: "Flaps",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Users_UserId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Flaps_Users_UserId",
                table: "Flaps");

            migrationBuilder.DropIndex(
                name: "IX_Flaps_UserId",
                table: "Flaps");

            migrationBuilder.DropIndex(
                name: "IX_Comments_UserId",
                table: "Comments");

            migrationBuilder.AddColumn<string>(
                name: "ImgUrl",
                table: "Flaps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nickname",
                table: "Flaps",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "Flaps",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
