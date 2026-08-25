using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzkFireTakibiClient.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReportScopeAddCategorySignature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_report_periods_Scope_EndDate",
                table: "report_periods");

            migrationBuilder.AddColumn<string>(
                name: "CategorySignature",
                table: "report_periods",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE period
                SET [CategorySignature] = LOWER(CONVERT(varchar(64), HASHBYTES(
                    'SHA2_256',
                    CONVERT(varchar(max), category_set.[CanonicalCodes])), 2))
                FROM [report_periods] AS period
                CROSS APPLY
                (
                    SELECT STRING_AGG(category.[CategoryCode], CHAR(31))
                        WITHIN GROUP (ORDER BY category.[CategoryCode]) AS [CanonicalCodes]
                    FROM
                    (
                        SELECT DISTINCT UPPER(LTRIM(RTRIM(row_data.[Kategori Kodu]))) AS [CategoryCode]
                        FROM [report_rows] AS row_data
                        WHERE row_data.[ReportImportId] =
                        (
                            SELECT TOP(1) report_import.[Id]
                            FROM [report_imports] AS report_import
                            WHERE report_import.[ReportPeriodId] = period.[Id]
                            ORDER BY report_import.[IsActive] DESC,
                                CASE WHEN report_import.[PeriodType] = N'Monthly' THEN 0 ELSE 1 END,
                                report_import.[CreatedAt] DESC
                        )
                        AND row_data.[RowType] = N'CategorySummary'
                    ) AS category
                ) AS category_set;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "CategorySignature",
                table: "report_periods",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "report_periods");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "report_imports");

            migrationBuilder.CreateIndex(
                name: "IX_report_periods_CategorySignature_EndDate",
                table: "report_periods",
                columns: new[] { "CategorySignature", "EndDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_report_periods_CategorySignature_EndDate",
                table: "report_periods");

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "report_periods",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "report_imports",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [report_periods]
                SET [Scope] = LEFT([CategorySignature], 40);

                UPDATE report_import
                SET [Scope] = period.[Scope]
                FROM [report_imports] AS report_import
                INNER JOIN [report_periods] AS period ON period.[Id] = report_import.[ReportPeriodId];
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Scope",
                table: "report_periods",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Scope",
                table: "report_imports",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "CategorySignature",
                table: "report_periods");

            migrationBuilder.CreateIndex(
                name: "IX_report_periods_Scope_EndDate",
                table: "report_periods",
                columns: new[] { "Scope", "EndDate" },
                unique: true);
        }
    }
}
