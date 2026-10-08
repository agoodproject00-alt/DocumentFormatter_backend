using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentFormatter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPredefinedActionOwners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PredefinedActions_IsActive_SortOrder",
                table: "PredefinedActions");

            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                table: "PredefinedActions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "PredefinedActions",
                keyColumn: "Id",
                keyValue: 1,
                column: "OwnerUserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "PredefinedActions",
                keyColumn: "Id",
                keyValue: 2,
                column: "OwnerUserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "PredefinedActions",
                keyColumn: "Id",
                keyValue: 3,
                column: "OwnerUserId",
                value: null);

            migrationBuilder.UpdateData(
                table: "PredefinedActions",
                keyColumn: "Id",
                keyValue: 4,
                column: "OwnerUserId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_PredefinedActions_OwnerUserId_IsActive_SortOrder",
                table: "PredefinedActions",
                columns: new[] { "OwnerUserId", "IsActive", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_PredefinedActions_AspNetUsers_OwnerUserId",
                table: "PredefinedActions",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PredefinedActions_AspNetUsers_OwnerUserId",
                table: "PredefinedActions");

            migrationBuilder.DropIndex(
                name: "IX_PredefinedActions_OwnerUserId_IsActive_SortOrder",
                table: "PredefinedActions");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "PredefinedActions");

            migrationBuilder.CreateIndex(
                name: "IX_PredefinedActions_IsActive_SortOrder",
                table: "PredefinedActions",
                columns: new[] { "IsActive", "SortOrder" });
        }
    }
}
