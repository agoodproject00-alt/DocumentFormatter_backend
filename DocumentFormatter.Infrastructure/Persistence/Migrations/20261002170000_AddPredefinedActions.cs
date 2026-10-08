using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentFormatter.Infrastructure.Persistence.Migrations;

public partial class AddPredefinedActions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PredefinedActions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Prompt = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_PredefinedActions", action => action.Id));

        migrationBuilder.CreateIndex(
            name: "IX_PredefinedActions_IsActive_SortOrder",
            table: "PredefinedActions",
            columns: new[] { "IsActive", "SortOrder" });

        migrationBuilder.InsertData(
            table: "PredefinedActions",
            columns: new[] { "Id", "IsActive", "Label", "Prompt", "SortOrder" },
            values: new object[,]
            {
                { 1, true, "Format this page", "Format this document to improve its layout and consistency while preserving its content.", 1 },
                { 2, true, "Resize this page", "Resize the pages in this document to a suitable standard page size while preserving the content.", 2 },
                { 3, true, "Change colour combination", "Update the document colour combination to a professional, accessible palette while preserving its content.", 3 },
                { 4, true, "Adjust margin", "Adjust the document margins for a clean, balanced layout while preserving its content.", 4 }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PredefinedActions");
    }
}
