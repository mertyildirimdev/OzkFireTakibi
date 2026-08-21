using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OzkFireTakibiClient.Migrations
{
    /// <inheritdoc />
    public partial class UseExcelColumnNamesForReportRows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WasteRate",
                table: "report_rows",
                newName: "Fire Oranı");

            migrationBuilder.RenameColumn(
                name: "WasteQuantity",
                table: "report_rows",
                newName: "Fire Miktarı");

            migrationBuilder.RenameColumn(
                name: "WasteAmount",
                table: "report_rows",
                newName: "Fire Tutarı");

            migrationBuilder.RenameColumn(
                name: "WarehouseTransferOutQuantity",
                table: "report_rows",
                newName: "Depo Sevk Satış Miktar");

            migrationBuilder.RenameColumn(
                name: "WarehouseTransferOutAmount",
                table: "report_rows",
                newName: "Depo Sevk Satış Tutar");

            migrationBuilder.RenameColumn(
                name: "WarehouseTransferInQuantity",
                table: "report_rows",
                newName: "Depo Sevk Alış Miktar");

            migrationBuilder.RenameColumn(
                name: "WarehouseTransferInAmount",
                table: "report_rows",
                newName: "Depo Sevk Alış Tutar");

            migrationBuilder.RenameColumn(
                name: "StoreSalesQuantity",
                table: "report_rows",
                newName: "Mağaza Satış Miktar");

            migrationBuilder.RenameColumn(
                name: "StoreSalesAmount",
                table: "report_rows",
                newName: "Mağaza Satış Tutar");

            migrationBuilder.RenameColumn(
                name: "StoreNumber",
                table: "report_rows",
                newName: "Depo No");

            migrationBuilder.RenameColumn(
                name: "StoreName",
                table: "report_rows",
                newName: "Depo Adı");

            migrationBuilder.RenameColumn(
                name: "StockName",
                table: "report_rows",
                newName: "Stok İsmi");

            migrationBuilder.RenameColumn(
                name: "StockCode",
                table: "report_rows",
                newName: "Stok Kodu");

            migrationBuilder.RenameColumn(
                name: "SourceReportType",
                table: "report_rows",
                newName: "rpr_tip");

            migrationBuilder.RenameColumn(
                name: "PurchaseStockValueFactor",
                table: "report_rows",
                newName: "Satın Alma Stok Değer Çarpanı");

            migrationBuilder.RenameColumn(
                name: "PurchaseGroupValueFactor",
                table: "report_rows",
                newName: "Satın Alma Grubu Değer Çarpanı");

            migrationBuilder.RenameColumn(
                name: "ProfitRate",
                table: "report_rows",
                newName: "Kar Oran");

            migrationBuilder.RenameColumn(
                name: "ProfitAmount",
                table: "report_rows",
                newName: "Kar Tutar");

            migrationBuilder.RenameColumn(
                name: "OpeningQuantity",
                table: "report_rows",
                newName: "Dönem Başı Miktar");

            migrationBuilder.RenameColumn(
                name: "OpeningAmount",
                table: "report_rows",
                newName: "Dönem Başı Tutar");

            migrationBuilder.RenameColumn(
                name: "CostOfSales",
                table: "report_rows",
                newName: "Satış Maliyeti");

            migrationBuilder.RenameColumn(
                name: "CostGroupType",
                table: "report_rows",
                newName: "Maliyet Grup Tipi");

            migrationBuilder.RenameColumn(
                name: "CostGroupCode",
                table: "report_rows",
                newName: "Maliyet Grup Kodu");

            migrationBuilder.RenameColumn(
                name: "CompanyPurchaseQuantity",
                table: "report_rows",
                newName: "Firma Alış Miktar");

            migrationBuilder.RenameColumn(
                name: "CompanyPurchaseAmount",
                table: "report_rows",
                newName: "Firma Alış Tutar");

            migrationBuilder.RenameColumn(
                name: "ClosingQuantity",
                table: "report_rows",
                newName: "Dönem Sonu Miktar");

            migrationBuilder.RenameColumn(
                name: "ClosingAmount",
                table: "report_rows",
                newName: "Dönem Sonu Tutar");

            migrationBuilder.RenameColumn(
                name: "CategoryWasteRate",
                table: "report_rows",
                newName: "Kategori Fire Oran");

            migrationBuilder.RenameColumn(
                name: "CategoryProfitRate",
                table: "report_rows",
                newName: "Kategori Kar Oran");

            migrationBuilder.RenameColumn(
                name: "CategoryName",
                table: "report_rows",
                newName: "Kategori İsmi");

            migrationBuilder.RenameColumn(
                name: "CategoryCode",
                table: "report_rows",
                newName: "Kategori Kodu");

            migrationBuilder.RenameColumn(
                name: "AlternativeName",
                table: "report_rows",
                newName: "Alternatif İsim");

            migrationBuilder.RenameIndex(
                name: "IX_report_rows_ReportImportId_StoreNumber",
                table: "report_rows",
                newName: "IX_report_rows_ReportImportId_Depo No");

            migrationBuilder.RenameIndex(
                name: "IX_report_rows_ReportImportId_StockCode",
                table: "report_rows",
                newName: "IX_report_rows_ReportImportId_Stok Kodu");

            migrationBuilder.RenameIndex(
                name: "IX_report_rows_ReportImportId_CategoryCode",
                table: "report_rows",
                newName: "IX_report_rows_ReportImportId_Kategori Kodu");

            migrationBuilder.AddColumn<int>(
                name: "rpr_id",
                table: "report_rows",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE report_rows
                SET rpr_id = CASE RowType
                    WHEN 'General' THEN 1
                    WHEN 'CategorySummary' THEN 2
                    WHEN 'StoreSummary' THEN 3
                    WHEN 'StoreCategory' THEN 4
                    WHEN 'ProductSummary' THEN 5
                    WHEN 'StoreProduct' THEN 7
                    ELSE 0
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rpr_id",
                table: "report_rows");

            migrationBuilder.RenameColumn(
                name: "rpr_tip",
                table: "report_rows",
                newName: "SourceReportType");

            migrationBuilder.RenameColumn(
                name: "Stok İsmi",
                table: "report_rows",
                newName: "StockName");

            migrationBuilder.RenameColumn(
                name: "Stok Kodu",
                table: "report_rows",
                newName: "StockCode");

            migrationBuilder.RenameColumn(
                name: "Satış Maliyeti",
                table: "report_rows",
                newName: "CostOfSales");

            migrationBuilder.RenameColumn(
                name: "Satın Alma Stok Değer Çarpanı",
                table: "report_rows",
                newName: "PurchaseStockValueFactor");

            migrationBuilder.RenameColumn(
                name: "Satın Alma Grubu Değer Çarpanı",
                table: "report_rows",
                newName: "PurchaseGroupValueFactor");

            migrationBuilder.RenameColumn(
                name: "Mağaza Satış Tutar",
                table: "report_rows",
                newName: "StoreSalesAmount");

            migrationBuilder.RenameColumn(
                name: "Mağaza Satış Miktar",
                table: "report_rows",
                newName: "StoreSalesQuantity");

            migrationBuilder.RenameColumn(
                name: "Maliyet Grup Tipi",
                table: "report_rows",
                newName: "CostGroupType");

            migrationBuilder.RenameColumn(
                name: "Maliyet Grup Kodu",
                table: "report_rows",
                newName: "CostGroupCode");

            migrationBuilder.RenameColumn(
                name: "Kategori İsmi",
                table: "report_rows",
                newName: "CategoryName");

            migrationBuilder.RenameColumn(
                name: "Kategori Kodu",
                table: "report_rows",
                newName: "CategoryCode");

            migrationBuilder.RenameColumn(
                name: "Kategori Kar Oran",
                table: "report_rows",
                newName: "CategoryProfitRate");

            migrationBuilder.RenameColumn(
                name: "Kategori Fire Oran",
                table: "report_rows",
                newName: "CategoryWasteRate");

            migrationBuilder.RenameColumn(
                name: "Kar Tutar",
                table: "report_rows",
                newName: "ProfitAmount");

            migrationBuilder.RenameColumn(
                name: "Kar Oran",
                table: "report_rows",
                newName: "ProfitRate");

            migrationBuilder.RenameColumn(
                name: "Firma Alış Tutar",
                table: "report_rows",
                newName: "CompanyPurchaseAmount");

            migrationBuilder.RenameColumn(
                name: "Firma Alış Miktar",
                table: "report_rows",
                newName: "CompanyPurchaseQuantity");

            migrationBuilder.RenameColumn(
                name: "Fire Tutarı",
                table: "report_rows",
                newName: "WasteAmount");

            migrationBuilder.RenameColumn(
                name: "Fire Oranı",
                table: "report_rows",
                newName: "WasteRate");

            migrationBuilder.RenameColumn(
                name: "Fire Miktarı",
                table: "report_rows",
                newName: "WasteQuantity");

            migrationBuilder.RenameColumn(
                name: "Dönem Sonu Tutar",
                table: "report_rows",
                newName: "ClosingAmount");

            migrationBuilder.RenameColumn(
                name: "Dönem Sonu Miktar",
                table: "report_rows",
                newName: "ClosingQuantity");

            migrationBuilder.RenameColumn(
                name: "Dönem Başı Tutar",
                table: "report_rows",
                newName: "OpeningAmount");

            migrationBuilder.RenameColumn(
                name: "Dönem Başı Miktar",
                table: "report_rows",
                newName: "OpeningQuantity");

            migrationBuilder.RenameColumn(
                name: "Depo Sevk Satış Tutar",
                table: "report_rows",
                newName: "WarehouseTransferOutAmount");

            migrationBuilder.RenameColumn(
                name: "Depo Sevk Satış Miktar",
                table: "report_rows",
                newName: "WarehouseTransferOutQuantity");

            migrationBuilder.RenameColumn(
                name: "Depo Sevk Alış Tutar",
                table: "report_rows",
                newName: "WarehouseTransferInAmount");

            migrationBuilder.RenameColumn(
                name: "Depo Sevk Alış Miktar",
                table: "report_rows",
                newName: "WarehouseTransferInQuantity");

            migrationBuilder.RenameColumn(
                name: "Depo No",
                table: "report_rows",
                newName: "StoreNumber");

            migrationBuilder.RenameColumn(
                name: "Depo Adı",
                table: "report_rows",
                newName: "StoreName");

            migrationBuilder.RenameColumn(
                name: "Alternatif İsim",
                table: "report_rows",
                newName: "AlternativeName");

            migrationBuilder.RenameIndex(
                name: "IX_report_rows_ReportImportId_Stok Kodu",
                table: "report_rows",
                newName: "IX_report_rows_ReportImportId_StockCode");

            migrationBuilder.RenameIndex(
                name: "IX_report_rows_ReportImportId_Kategori Kodu",
                table: "report_rows",
                newName: "IX_report_rows_ReportImportId_CategoryCode");

            migrationBuilder.RenameIndex(
                name: "IX_report_rows_ReportImportId_Depo No",
                table: "report_rows",
                newName: "IX_report_rows_ReportImportId_StoreNumber");
        }
    }
}
