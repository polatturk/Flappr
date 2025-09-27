using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Flappr.Migrations
{
    /// <inheritdoc />
    public partial class AddFollowTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "YorumSayisi",
                table: "Flaps",
                newName: "LikeCount");

            migrationBuilder.AddColumn<int>(
                name: "CommentCount",
                table: "Flaps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Follows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FollowerId = table.Column<int>(type: "int", nullable: false),
                    FollowingId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Follows", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Follows");

            migrationBuilder.DropColumn(
                name: "CommentCount",
                table: "Flaps");

            migrationBuilder.RenameColumn(
                name: "LikeCount",
                table: "Flaps",
                newName: "YorumSayisi");
        }
    }
}
