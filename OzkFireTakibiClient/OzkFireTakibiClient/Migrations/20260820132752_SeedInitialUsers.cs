using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OzkFireTakibiClient.Migrations;

/// <inheritdoc />
public partial class SeedInitialUsers : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            table: "users",
            columns: new[] { "Id", "CreatedAt", "Email", "IsDeleted", "Name", "Password", "Role", "StoreName", "UpdatedAt" },
            values: new object[,]
            {
                { 1, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), "admin@ozkfiretakibi.local", false, "System Admin", "admin123", "Admin", null, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                { 2, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), "user@ozkfiretakibi.local", false, "Normal User", "user123", "User", null, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc) }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            table: "users",
            keyColumn: "Id",
            keyValue: 1);

        migrationBuilder.DeleteData(
            table: "users",
            keyColumn: "Id",
            keyValue: 2);
    }
}
