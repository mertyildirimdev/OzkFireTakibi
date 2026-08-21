using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace OzkFireTakibiClient.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "report_periods",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Scope = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_periods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "stores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    IsExcuseEligible = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StoreName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StoreNumber = table.Column<int>(type: "int", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "report_imports",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportPeriodId = table.Column<long>(type: "bigint", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PeriodType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FileHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "int", nullable: false),
                    TotalRowCount = table.Column<int>(type: "int", nullable: false),
                    GeneralRowCount = table.Column<int>(type: "int", nullable: false),
                    CategorySummaryRowCount = table.Column<int>(type: "int", nullable: false),
                    StoreSummaryRowCount = table.Column<int>(type: "int", nullable: false),
                    StoreCategoryRowCount = table.Column<int>(type: "int", nullable: false),
                    ProductSummaryRowCount = table.Column<int>(type: "int", nullable: false),
                    StoreProductRowCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_imports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_report_imports_report_periods_ReportPeriodId",
                        column: x => x.ReportPeriodId,
                        principalTable: "report_periods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportImportId = table.Column<long>(type: "bigint", nullable: false),
                    SourceRowNumber = table.Column<int>(type: "int", nullable: false),
                    RowType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    rpr_id = table.Column<int>(type: "int", nullable: false),
                    rpr_tip = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DepoNo = table.Column<int>(name: "Depo No", type: "int", nullable: true),
                    DepoAdı = table.Column<string>(name: "Depo Adı", type: "nvarchar(160)", maxLength: 160, nullable: true),
                    KategoriKodu = table.Column<string>(name: "Kategori Kodu", type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Kategoriİsmi = table.Column<string>(name: "Kategori İsmi", type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StokKodu = table.Column<string>(name: "Stok Kodu", type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Stokİsmi = table.Column<string>(name: "Stok İsmi", type: "nvarchar(240)", maxLength: 240, nullable: true),
                    Alternatifİsim = table.Column<string>(name: "Alternatif İsim", type: "nvarchar(240)", maxLength: 240, nullable: true),
                    MaliyetGrupTipi = table.Column<string>(name: "Maliyet Grup Tipi", type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MaliyetGrupKodu = table.Column<string>(name: "Maliyet Grup Kodu", type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SatınAlmaGrubuDeğerÇarpanı = table.Column<decimal>(name: "Satın Alma Grubu Değer Çarpanı", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    SatınAlmaStokDeğerÇarpanı = table.Column<decimal>(name: "Satın Alma Stok Değer Çarpanı", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    DönemBaşıMiktar = table.Column<decimal>(name: "Dönem Başı Miktar", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    DönemBaşıTutar = table.Column<decimal>(name: "Dönem Başı Tutar", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    FirmaAlışMiktar = table.Column<decimal>(name: "Firma Alış Miktar", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    FirmaAlışTutar = table.Column<decimal>(name: "Firma Alış Tutar", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    DepoSevkAlışMiktar = table.Column<decimal>(name: "Depo Sevk Alış Miktar", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    DepoSevkAlışTutar = table.Column<decimal>(name: "Depo Sevk Alış Tutar", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    DepoSevkSatışMiktar = table.Column<decimal>(name: "Depo Sevk Satış Miktar", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    DepoSevkSatışTutar = table.Column<decimal>(name: "Depo Sevk Satış Tutar", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    MağazaSatışMiktar = table.Column<decimal>(name: "Mağaza Satış Miktar", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    MağazaSatışTutar = table.Column<decimal>(name: "Mağaza Satış Tutar", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    SatışMaliyeti = table.Column<decimal>(name: "Satış Maliyeti", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    FireOranı = table.Column<decimal>(name: "Fire Oranı", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    FireMiktarı = table.Column<decimal>(name: "Fire Miktarı", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    FireTutarı = table.Column<decimal>(name: "Fire Tutarı", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    DönemSonuMiktar = table.Column<decimal>(name: "Dönem Sonu Miktar", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    DönemSonuTutar = table.Column<decimal>(name: "Dönem Sonu Tutar", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    KarTutar = table.Column<decimal>(name: "Kar Tutar", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    KarOran = table.Column<decimal>(name: "Kar Oran", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    KategoriKarOran = table.Column<decimal>(name: "Kategori Kar Oran", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    KategoriFireOran = table.Column<decimal>(name: "Kategori Fire Oran", type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "excuse_requests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportRowId = table.Column<long>(type: "bigint", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    RequestNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequestedByUserId = table.Column<int>(type: "int", nullable: true),
                    ThresholdRate = table.Column<decimal>(type: "decimal(20,6)", precision: 20, scale: 6, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StatusBeforeSuperseded = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SupersededByReportImportId = table.Column<long>(type: "bigint", nullable: true),
                    RespondedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_excuse_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_excuse_requests_report_rows_ReportRowId",
                        column: x => x.ReportRowId,
                        principalTable: "report_rows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_excuse_requests_users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "excuse_entries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExcuseRequestId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    EntryType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReasonType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_excuse_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_excuse_entries_excuse_requests_ExcuseRequestId",
                        column: x => x.ExcuseRequestId,
                        principalTable: "excuse_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_excuse_entries_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "CreatedAt", "Email", "IsDeleted", "Name", "Password", "Role", "StoreName", "StoreNumber", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), "admin@ozkfiretakibi.local", false, "System Admin", "admin123", "Admin", null, null, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc), "user@ozkfiretakibi.local", false, "Normal User", "user123", "User", null, null, new DateTime(2026, 8, 20, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_excuse_entries_CreatedByUserId",
                table: "excuse_entries",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_excuse_entries_ExcuseRequestId_CreatedAt",
                table: "excuse_entries",
                columns: new[] { "ExcuseRequestId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_excuse_requests_ReportRowId",
                table: "excuse_requests",
                column: "ReportRowId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_excuse_requests_RequestedByUserId",
                table: "excuse_requests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_excuse_requests_Status_CreatedAt",
                table: "excuse_requests",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_report_imports_FileHash",
                table: "report_imports",
                column: "FileHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_imports_ReportPeriodId_PeriodType_IsActive",
                table: "report_imports",
                columns: new[] { "ReportPeriodId", "PeriodType", "IsActive" },
                unique: true,
                filter: "\"IsActive\" = 1");

            migrationBuilder.CreateIndex(
                name: "IX_report_imports_UploadedByUserId",
                table: "report_imports",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_report_periods_Scope_EndDate",
                table: "report_periods",
                columns: new[] { "Scope", "EndDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_report_rows_Depo No_RowType",
                table: "report_rows",
                columns: new[] { "Depo No", "RowType" });

            migrationBuilder.CreateIndex(
                name: "IX_report_rows_ReportImportId_Depo No",
                table: "report_rows",
                columns: new[] { "ReportImportId", "Depo No" });

            migrationBuilder.CreateIndex(
                name: "IX_report_rows_ReportImportId_Kategori Kodu",
                table: "report_rows",
                columns: new[] { "ReportImportId", "Kategori Kodu" });

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
                name: "IX_report_rows_ReportImportId_Stok Kodu",
                table: "report_rows",
                columns: new[] { "ReportImportId", "Stok Kodu" });

            migrationBuilder.CreateIndex(
                name: "IX_users_StoreNumber",
                table: "users",
                column: "StoreNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "excuse_entries");

            migrationBuilder.DropTable(
                name: "stores");

            migrationBuilder.DropTable(
                name: "excuse_requests");

            migrationBuilder.DropTable(
                name: "report_rows");

            migrationBuilder.DropTable(
                name: "report_imports");

            migrationBuilder.DropTable(
                name: "report_periods");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
