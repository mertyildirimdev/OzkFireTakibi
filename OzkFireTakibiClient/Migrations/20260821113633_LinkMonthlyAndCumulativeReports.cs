using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzkFireTakibiClient.Migrations
{
    /// <inheritdoc />
    public partial class LinkMonthlyAndCumulativeReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_report_imports_Scope_StartDate_EndDate_PeriodType_IsActive",
                table: "report_imports");

            migrationBuilder.AddColumn<long>(
                name: "ReportPeriodId",
                table: "report_imports",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "report_periods",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Scope = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_periods", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO report_periods (Scope, EndDate, CreatedAt, UpdatedAt)
                SELECT Scope, EndDate, MIN(CreatedAt), MAX(UpdatedAt)
                FROM report_imports
                GROUP BY Scope, EndDate;

                UPDATE report_imports
                SET ReportPeriodId = (
                    SELECT report_periods.Id
                    FROM report_periods
                    WHERE report_periods.Scope = report_imports.Scope
                      AND report_periods.EndDate = report_imports.EndDate
                );

                UPDATE report_imports
                SET IsActive = 0,
                    UpdatedAt = CURRENT_TIMESTAMP
                WHERE IsActive = 1
                  AND EXISTS (
                      SELECT 1
                      FROM report_imports AS newer
                      WHERE newer.ReportPeriodId = report_imports.ReportPeriodId
                        AND newer.PeriodType = report_imports.PeriodType
                        AND newer.IsActive = 1
                        AND (
                            newer.CreatedAt > report_imports.CreatedAt
                            OR (newer.CreatedAt = report_imports.CreatedAt AND newer.Id > report_imports.Id)
                        )
                  );
                """);

            migrationBuilder.CreateIndex(
                name: "IX_report_imports_ReportPeriodId_PeriodType_IsActive",
                table: "report_imports",
                columns: new[] { "ReportPeriodId", "PeriodType", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_report_periods_Scope_EndDate",
                table: "report_periods",
                columns: new[] { "Scope", "EndDate" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_report_imports_report_periods_ReportPeriodId",
                table: "report_imports",
                column: "ReportPeriodId",
                principalTable: "report_periods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_report_imports_report_periods_ReportPeriodId",
                table: "report_imports");

            migrationBuilder.DropTable(
                name: "report_periods");

            migrationBuilder.DropIndex(
                name: "IX_report_imports_ReportPeriodId_PeriodType_IsActive",
                table: "report_imports");

            migrationBuilder.DropColumn(
                name: "ReportPeriodId",
                table: "report_imports");

            migrationBuilder.CreateIndex(
                name: "IX_report_imports_Scope_StartDate_EndDate_PeriodType_IsActive",
                table: "report_imports",
                columns: new[] { "Scope", "StartDate", "EndDate", "PeriodType", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = 1");
        }
    }
}
