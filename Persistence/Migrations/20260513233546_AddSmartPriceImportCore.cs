using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartPriceImportCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PriceImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SourceFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SourceFilePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SourceFileHash = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FileType = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalCandidates = table.Column<int>(type: "int", nullable: false),
                    ReadyCount = table.Column<int>(type: "int", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    ApprovedCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceImportMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ExternalColumnName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    InternalFieldName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceImportMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceLists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceFilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceFileHash = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceImportCandidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ImportJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArticleNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ProductCode = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CategoryName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    NetPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PageNumber = table.Column<int>(type: "int", nullable: true),
                    SheetName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CellRange = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceImportCandidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceImportCandidates_PriceImportJobs_ImportJobId",
                        column: x => x.ImportJobId,
                        principalTable: "PriceImportJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PriceListItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PriceListId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ArticleNumber = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ProductCode = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CategoryName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClassificationPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    NetPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ConsumptionFactor = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    WastePercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    SourceFileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourcePageNumber = table.Column<int>(type: "int", nullable: true),
                    SourceSheetName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceCellRange = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SourceText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UserNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceListItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceListItems_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceImportCandidates_ImportJobId",
                table: "PriceImportCandidates",
                column: "ImportJobId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceImportCandidates_TenantId_ArticleNumber",
                table: "PriceImportCandidates",
                columns: new[] { "TenantId", "ArticleNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceImportCandidates_TenantId_ImportJobId_Status",
                table: "PriceImportCandidates",
                columns: new[] { "TenantId", "ImportJobId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceImportCandidates_TenantId_Name",
                table: "PriceImportCandidates",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceImportCandidates_TenantId_ProductCode",
                table: "PriceImportCandidates",
                columns: new[] { "TenantId", "ProductCode" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceImportCandidates_TenantId_Status",
                table: "PriceImportCandidates",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceImportJobs_TenantId_SourceFileHash",
                table: "PriceImportJobs",
                columns: new[] { "TenantId", "SourceFileHash" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceImportJobs_TenantId_Status",
                table: "PriceImportJobs",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceImportJobs_TenantId_SupplierName",
                table: "PriceImportJobs",
                columns: new[] { "TenantId", "SupplierName" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceImportMappings_TenantId_SupplierName_ExternalColumnName",
                table: "PriceImportMappings",
                columns: new[] { "TenantId", "SupplierName", "ExternalColumnName" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_PriceListId",
                table: "PriceListItems",
                column: "PriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_TenantId_IsActive",
                table: "PriceListItems",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_TenantId_Name",
                table: "PriceListItems",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_TenantId_PriceListId",
                table: "PriceListItems",
                columns: new[] { "TenantId", "PriceListId" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_TenantId_PriceListId_ArticleNumber",
                table: "PriceListItems",
                columns: new[] { "TenantId", "PriceListId", "ArticleNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_TenantId_ProductCode",
                table: "PriceListItems",
                columns: new[] { "TenantId", "ProductCode" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceListItems_TenantId_SupplierName",
                table: "PriceListItems",
                columns: new[] { "TenantId", "SupplierName" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_TenantId_IsActive",
                table: "PriceLists",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_TenantId_SourceFileHash",
                table: "PriceLists",
                columns: new[] { "TenantId", "SourceFileHash" });

            migrationBuilder.CreateIndex(
                name: "IX_PriceLists_TenantId_SupplierName",
                table: "PriceLists",
                columns: new[] { "TenantId", "SupplierName" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceImportCandidates");

            migrationBuilder.DropTable(
                name: "PriceImportMappings");

            migrationBuilder.DropTable(
                name: "PriceListItems");

            migrationBuilder.DropTable(
                name: "PriceImportJobs");

            migrationBuilder.DropTable(
                name: "PriceLists");
        }
    }
}
