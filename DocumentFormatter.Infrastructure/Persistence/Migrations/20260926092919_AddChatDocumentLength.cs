using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentFormatter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChatDocumentLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Length",
                table: "ChatDocuments",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Length",
                table: "ChatDocuments");
        }
    }
}
