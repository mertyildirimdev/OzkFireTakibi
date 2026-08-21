using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzkFireTakibiClient.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionedReportImports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "report_imports",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Scope = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    PeriodType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    OriginalFileName = table.Column<string>(type: "TEXT", maxLength: 260, nullable: false),
                    FileHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalRowCount = table.Column<int>(type: "INTEGER", nullable: false),
                    GeneralRowCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CategorySummaryRowCount = table.Column<int>(type: "INTEGER", nullable: false),
                    StoreSummaryRowCount = table.Column<int>(type: "INTEGER", nullable: false),
                    StoreCategoryRowCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductSummaryRowCount = table.Column<int>(type: "INTEGER", nullable: false),
                    StoreProductRowCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_imports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_imports_users_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "report_rows",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReportImportId = table.Column<long>(type: "INTEGER", nullable: false),
                    SourceRowNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    RowType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    SourceReportType = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    StoreNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    StoreName = table.Column<string>(type: "TEXT", maxLength: 160, nullable: true),
                    CategoryCode = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    CategoryName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    StockCode = table.Column<string>(type: "TEXT", maxLength: 60, nullable: true),
                    StockName = table.Column<string>(type: "TEXT", maxLength: 240, nullable: true),
                    AlternativeName = table.Column<string>(type: "TEXT", maxLength: 240, nullable: true),
                    CostGroupType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CostGroupCode = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PurchaseGroupValueFactor = table.Column<double>(type: "REAL", nullable: true),
                    PurchaseStockValueFactor = table.Column<double>(type: "REAL", nullable: true),
                    OpeningQuantity = table.Column<double>(type: "REAL", nullable: true),
                    OpeningAmount = table.Column<double>(type: "REAL", nullable: true),
                    CompanyPurchaseQuantity = table.Column<double>(type: "REAL", nullable: true),
                    CompanyPurchaseAmount = table.Column<double>(type: "REAL", nullable: true),
                    WarehouseTransferInQuantity = table.Column<double>(type: "REAL", nullable: true),
                    WarehouseTransferInAmount = table.Column<double>(type: "REAL", nullable: true),
                    WarehouseTransferOutQuantity = table.Column<double>(type: "REAL", nullable: true),
                    WarehouseTransferOutAmount = table.Column<double>(type: "REAL", nullable: true),
                    StoreSalesQuantity = table.Column<double>(type: "REAL", nullable: true),
                    StoreSalesAmount = table.Column<double>(type: "REAL", nullable: true),
                    CostOfSales = table.Column<double>(type: "REAL", nullable: true),
                    WasteRate = table.Column<double>(type: "REAL", nullable: true),
                    WasteQuantity = table.Column<double>(type: "REAL", nullable: true),
                    WasteAmount = table.Column<double>(type: "REAL", nullable: true),
                    ClosingQuantity = table.Column<double>(type: "REAL", nullable: true),
                    ClosingAmount = table.Column<double>(type: "REAL", nullable: true),
                    ProfitAmount = table.Column<double>(type: "REAL", nullable: true),
                    ProfitRate = table.Column<double>(type: "REAL", nullable: true),
                    CategoryProfitRate = table.Column<double>(type: "REAL", nullable: true),
                    CategoryWasteRate = table.Column<double>(type: "REAL", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_rows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_rows_report_imports_ReportImportId",
                        column: x => x.ReportImportId,
                        principalTable: "report_imports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_report_imports_FileHash",
                table: "report_imports",
                column: "FileHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_imports_Scope_StartDate_EndDate_PeriodType_IsActive",
                table: "report_imports",
                columns: new[] { "Scope", "StartDate", "EndDate", "PeriodType", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_report_imports_UploadedByUserId",
                table: "report_imports",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_report_rows_ReportImportId_CategoryCode",
                table: "report_rows",
                columns: new[] { "ReportImportId", "CategoryCode" });

            migrationBuilder.CreateIndex(
                name: "IX_report_rows_ReportImportId_RowType",
                table: "report_rows",
                columns: new[] { "ReportImportId", "RowType" });

            migrationBuilder.CreateIndex(
                name: "IX_report_rows_ReportImportId_SourceRowNumber",
                table: "report_rows",
                columns: new[] { "ReportImportId", "SourceRowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_rows_ReportImportId_StockCode",
                table: "report_rows",
                columns: new[] { "ReportImportId", "StockCode" });

            migrationBuilder.CreateIndex(
                name: "IX_report_rows_ReportImportId_StoreNumber",
                table: "report_rows",
                columns: new[] { "ReportImportId", "StoreNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_rows");

            migrationBuilder.DropTable(
                name: "report_imports");
        }
    }
}
