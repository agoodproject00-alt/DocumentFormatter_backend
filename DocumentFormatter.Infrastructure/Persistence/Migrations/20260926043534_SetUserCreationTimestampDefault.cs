using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DocumentFormatter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SetUserCreationTimestampDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset");

            migrationBuilder.Sql("UPDATE [AspNetUsers] SET [CreatedAtUtc] = SYSUTCDATETIME() WHERE [CreatedAtUtc] = '0001-01-01T00:00:00.0000000+00:00';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CreatedAtUtc",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "datetimeoffset",
                oldDefaultValueSql: "SYSUTCDATETIME()");
        }
    }
}
