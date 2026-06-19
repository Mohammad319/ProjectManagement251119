using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DB260617_3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "OrderSeq",
                startValue: 0L,
                incrementBy: 100);

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
                    table.CheckConstraint("CK_PriceImportJobs_Counts_NonNegative", "[TotalCandidates] >= 0 AND [ReadyCount] >= 0 AND [ReviewCount] >= 0 AND [ErrorCount] >= 0 AND [ApprovedCount] >= 0");
                    table.CheckConstraint("CK_PriceImportJobs_DateRange", "[CompletedAt] IS NULL OR [CompletedAt] >= [StartedAt]");
                    table.CheckConstraint("CK_PriceImportJobs_SourceFileName_NotEmpty", "LEN(LTRIM(RTRIM([SourceFileName]))) > 0");
                    table.CheckConstraint("CK_PriceImportJobs_SourceFilePath_NotEmpty", "LEN(LTRIM(RTRIM([SourceFilePath]))) > 0");
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
                    table.CheckConstraint("CK_PriceImportMappings_ExternalColumnName_NotEmpty", "LEN(LTRIM(RTRIM([ExternalColumnName]))) > 0");
                    table.CheckConstraint("CK_PriceImportMappings_InternalFieldName_NotEmpty", "LEN(LTRIM(RTRIM([InternalFieldName]))) > 0");
                    table.CheckConstraint("CK_PriceImportMappings_SupplierName_NotEmpty", "LEN(LTRIM(RTRIM([SupplierName]))) > 0");
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
                    table.CheckConstraint("CK_PriceLists_DateRange", "[ValidTo] IS NULL OR [ValidFrom] IS NULL OR [ValidTo] >= [ValidFrom]");
                    table.CheckConstraint("CK_PriceLists_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                });

            migrationBuilder.CreateTable(
                name: "UserManagementAuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    TargetUserId = table.Column<int>(type: "int", nullable: true),
                    TargetAuthId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TargetDisplayName = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    ActorDisplayName = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserManagementAuditLogs", x => x.Id);
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
                    table.CheckConstraint("CK_PriceImportCandidates_BasePrice_NonNegative", "[BasePrice] IS NULL OR [BasePrice] >= 0");
                    table.CheckConstraint("CK_PriceImportCandidates_Confidence_Range", "[Confidence] >= 0 AND [Confidence] <= 1");
                    table.CheckConstraint("CK_PriceImportCandidates_DiscountPercent_Range", "[DiscountPercent] IS NULL OR ([DiscountPercent] >= 0 AND [DiscountPercent] <= 100)");
                    table.CheckConstraint("CK_PriceImportCandidates_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_PriceImportCandidates_NetPrice_NonNegative", "[NetPrice] IS NULL OR [NetPrice] >= 0");
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
                    table.CheckConstraint("CK_PriceListItems_BasePrice_NonNegative", "[BasePrice] IS NULL OR [BasePrice] >= 0");
                    table.CheckConstraint("CK_PriceListItems_Confidence_Range", "[Confidence] IS NULL OR ([Confidence] >= 0 AND [Confidence] <= 1)");
                    table.CheckConstraint("CK_PriceListItems_ConsumptionFactor_NonNegative", "[ConsumptionFactor] IS NULL OR [ConsumptionFactor] >= 0");
                    table.CheckConstraint("CK_PriceListItems_DiscountPercent_Range", "[DiscountPercent] IS NULL OR ([DiscountPercent] >= 0 AND [DiscountPercent] <= 100)");
                    table.CheckConstraint("CK_PriceListItems_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_PriceListItems_NetPrice_NonNegative", "[NetPrice] IS NULL OR [NetPrice] >= 0");
                    table.CheckConstraint("CK_PriceListItems_WastePercent_Range", "[WastePercent] IS NULL OR ([WastePercent] >= 0 AND [WastePercent] <= 100)");
                    table.ForeignKey(
                        name: "FK_PriceListItems_PriceLists_PriceListId",
                        column: x => x.PriceListId,
                        principalTable: "PriceLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountGroups", x => x.Id);
                    table.CheckConstraint("CK_AccountGroups_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    AccountGroupId = table.Column<int>(type: "int", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.CheckConstraint("CK_Accounts_Code_NotEmpty", "LEN(LTRIM(RTRIM([Code]))) > 0");
                    table.CheckConstraint("CK_Accounts_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.ForeignKey(
                        name: "FK_Accounts_AccountGroups_AccountGroupId",
                        column: x => x.AccountGroupId,
                        principalTable: "AccountGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Applications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Applications", x => x.Id);
                    table.CheckConstraint("CK_Applications_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                });

            migrationBuilder.CreateTable(
                name: "ApplicationValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CalculationId = table.Column<int>(type: "int", nullable: false),
                    ApplicationId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Responsible = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastUpdate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationValues", x => x.Id);
                    table.CheckConstraint("CK_ApplicationValues_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.ForeignKey(
                        name: "FK_ApplicationValues_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Calculations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HourlyPrice = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Factors = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Tax = table.Column<int>(type: "int", nullable: false),
                    TenderDeadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenderQA = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Sort = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayPresets = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublicationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPrivate = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CalculationType = table.Column<int>(type: "int", nullable: false),
                    BidRole = table.Column<int>(type: "int", nullable: false),
                    CalculationRole = table.Column<int>(type: "int", nullable: false),
                    CustomCalculationRoleName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedByName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SourceCalculationId = table.Column<int>(type: "int", nullable: true),
                    VersionGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedFromCalculationId = table.Column<int>(type: "int", nullable: true),
                    IsCurrentVersion = table.Column<bool>(type: "bit", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: true),
                    TypeId = table.Column<int>(type: "int", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    ProcurementMethodsId = table.Column<int>(type: "int", nullable: true),
                    CompensationId = table.Column<int>(type: "int", nullable: true),
                    ContractId = table.Column<int>(type: "int", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: true),
                    TemplateColumnId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calculations", x => x.Id);
                    table.CheckConstraint("CK_Calculations_BidRole", "[BidRole] IN (0, 1, 2, 3)");
                    table.CheckConstraint("CK_Calculations_CalculationRole", "[CalculationRole] IN (0, 1, 2, 3, 4, 5)");
                    table.CheckConstraint("CK_Calculations_CalculationType", "[CalculationType] IN (0, 1, 2)");
                    table.CheckConstraint("CK_Calculations_Code_NotEmpty", "LEN(LTRIM(RTRIM([Code]))) > 0");
                    table.CheckConstraint("CK_Calculations_DateRange", "[EndDate] >= [StartDate]");
                    table.CheckConstraint("CK_Calculations_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_Calculations_Tax_0_100", "[Tax] >= 0 AND [Tax] <= 100");
                });

            migrationBuilder.CreateTable(
                name: "TenderAttributeDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    CalculationId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderAttributeDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderAttributeDefinitions_Calculations_CalculationId",
                        column: x => x.CalculationId,
                        principalTable: "Calculations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Compensations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR OrderSeq"),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compensations", x => x.Id);
                    table.CheckConstraint("CK_Compensations_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_Compensations_SortOrder_NonNegative", "[SortOrder] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR OrderSeq"),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                    table.CheckConstraint("CK_Contracts_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_Contracts_SortOrder_NonNegative", "[SortOrder] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    HeadUserId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                    table.CheckConstraint("CK_Departments_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalAuthId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_Users_Email_NotEmpty", "LEN(LTRIM(RTRIM([Email]))) > 0");
                    table.CheckConstraint("CK_Users_UserName_NotEmpty", "LEN(LTRIM(RTRIM([UserName]))) > 0");
                    table.ForeignKey(
                        name: "FK_Users_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Folders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folders", x => x.Id);
                    table.CheckConstraint("CK_Folders_Department_Positive", "[DepartmentId] > 0");
                    table.CheckConstraint("CK_Folders_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_Folders_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_Folders_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Folders_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Folders_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Opportunities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OpportunitiesRisks = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    OpportunityType = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    CalculationId = table.Column<int>(type: "int", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Opportunities", x => x.Id);
                    table.CheckConstraint("CK_Opportunities_Risks_NotEmpty", "LEN(LTRIM(RTRIM([OpportunitiesRisks]))) > 0");
                    table.ForeignKey(
                        name: "FK_Opportunities_Calculations_CalculationId",
                        column: x => x.CalculationId,
                        principalTable: "Calculations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Opportunities_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Opportunities_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganisationCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationCategories", x => x.Id);
                    table.CheckConstraint("CK_OrganisationCategories_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_OrganisationCategories_Parent_Positive", "[ParentCategoryId] IS NULL OR [ParentCategoryId] > 0");
                    table.ForeignKey(
                        name: "FK_OrganisationCategories_OrganisationCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "OrganisationCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganisationCategories_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganisationCategories_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganisationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganisationTypes", x => x.Id);
                    table.CheckConstraint("CK_OrganisationTypes_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.ForeignKey(
                        name: "FK_OrganisationTypes_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganisationTypes_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcurementMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR OrderSeq"),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcurementMethods", x => x.Id);
                    table.CheckConstraint("CK_ProcurementMethods_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_ProcurementMethods_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_ProcurementMethods_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcurementMethods_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcurementProcedures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcurementProcedures", x => x.Id);
                    table.CheckConstraint("CK_ProcurementProcedures_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_ProcurementProcedures_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_ProcurementProcedures_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcurementProcedures_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CountsAsSubmittedBid = table.Column<bool>(type: "bit", nullable: false),
                    CountsAsWonBid = table.Column<bool>(type: "bit", nullable: false),
                    CountsAsLostBid = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR OrderSeq"),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectStatuses", x => x.Id);
                    table.CheckConstraint("CK_ProjectStatuses_BidResult_NotWonAndLost", "[CountsAsWonBid] = 0 OR [CountsAsLostBid] = 0");
                    table.CheckConstraint("CK_ProjectStatuses_BidResult_RequiresSubmitted", "[CountsAsSubmittedBid] = 1 OR ([CountsAsWonBid] = 0 AND [CountsAsLostBid] = 0)");
                    table.CheckConstraint("CK_ProjectStatuses_Color_Hex", "[Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'");
                    table.CheckConstraint("CK_ProjectStatuses_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_ProjectStatuses_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_ProjectStatuses_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectStatuses_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR OrderSeq"),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTypes", x => x.Id);
                    table.CheckConstraint("CK_ProjectTypes_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_ProjectTypes_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_ProjectTypes_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectTypes_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResourceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR OrderSeq"),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceTypes", x => x.Id);
                    table.CheckConstraint("CK_ResourceTypes_Account_Positive", "[AccountId] IS NULL OR [AccountId] > 0");
                    table.CheckConstraint("CK_ResourceTypes_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_ResourceTypes_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_ResourceTypes_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResourceTypes_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceTypes_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShareCalcs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    CalculationId = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUserId = table.Column<int>(type: "int", nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareCalcs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareCalcs_Calculations_CalculationId",
                        column: x => x.CalculationId,
                        principalTable: "Calculations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShareCalcs_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShareCalcs_Users_CreatedAtUserId",
                        column: x => x.CreatedAtUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShareCalcs_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShareCalcs_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Statuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsApprovalStatus = table.Column<bool>(type: "bit", nullable: false),
                    LocksCalculation = table.Column<bool>(type: "bit", nullable: false),
                    AllowsProductionCalculation = table.Column<bool>(type: "bit", nullable: false),
                    CountsAsSubmittedBid = table.Column<bool>(type: "bit", nullable: false),
                    CountsAsWonBid = table.Column<bool>(type: "bit", nullable: false),
                    CountsAsLostBid = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR OrderSeq"),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statuses", x => x.Id);
                    table.CheckConstraint("CK_Statuses_BidResult_NotWonAndLost", "[CountsAsWonBid] = 0 OR [CountsAsLostBid] = 0");
                    table.CheckConstraint("CK_Statuses_BidResult_RequiresSubmitted", "[CountsAsSubmittedBid] = 1 OR ([CountsAsWonBid] = 0 AND [CountsAsLostBid] = 0)");
                    table.CheckConstraint("CK_Statuses_Color_Hex", "[Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'");
                    table.CheckConstraint("CK_Statuses_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_Statuses_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_Statuses_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Statuses_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StatusResources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsSystemDefault = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR OrderSeq"),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusResources", x => x.Id);
                    table.CheckConstraint("CK_StatusResources_Color_Hex", "[Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'");
                    table.CheckConstraint("CK_StatusResources_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_StatusResources_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_StatusResources_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StatusResources_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Storages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StorageValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    StorageType = table.Column<int>(type: "int", nullable: false),
                    StorageSort = table.Column<int>(type: "int", nullable: false),
                    StorageLevel = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Storages", x => x.Id);
                    table.CheckConstraint("CK_Storages_Value_NotEmpty", "LEN(LTRIM(RTRIM([StorageValue]))) > 0");
                    table.ForeignKey(
                        name: "FK_Storages_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Storages_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Storages_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    IsSystemDefault = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR OrderSeq"),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskStatuses", x => x.Id);
                    table.CheckConstraint("CK_TaskStatuses_Color_Hex", "[Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'");
                    table.CheckConstraint("CK_TaskStatuses_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_TaskStatuses_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_TaskStatuses_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskStatuses_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TemplateColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    Columns = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateColumns", x => x.Id);
                    table.CheckConstraint("CK_TemplateColumns_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.ForeignKey(
                        name: "FK_TemplateColumns_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateColumns_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TemplateColumns_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Templates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Templates", x => x.Id);
                    table.CheckConstraint("CK_Templates_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.ForeignKey(
                        name: "FK_Templates_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Templates_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Templates_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserListSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserListSettings", x => x.Id);
                    table.CheckConstraint("CK_UserListSettings_Kind_NotEmpty", "LEN(LTRIM(RTRIM([Kind]))) > 0");
                    table.CheckConstraint("CK_UserListSettings_Scope_NotEmpty", "LEN(LTRIM(RTRIM([Scope]))) > 0");
                    table.ForeignKey(
                        name: "FK_UserListSettings_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserListSettings_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Organisations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganisationCategoryId = table.Column<int>(type: "int", nullable: false),
                    OrganisationTypeId = table.Column<int>(type: "int", nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organisations", x => x.Id);
                    table.CheckConstraint("CK_Organisations_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.ForeignKey(
                        name: "FK_Organisations_OrganisationCategories_OrganisationCategoryId",
                        column: x => x.OrganisationCategoryId,
                        principalTable: "OrganisationCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Organisations_OrganisationTypes_OrganisationTypeId",
                        column: x => x.OrganisationTypeId,
                        principalTable: "OrganisationTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Organisations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Organisations_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResourceSorts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ResourceTypeId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceSorts", x => x.Id);
                    table.CheckConstraint("CK_ResourceSorts_Account_Positive", "[AccountId] IS NULL OR [AccountId] > 0");
                    table.CheckConstraint("CK_ResourceSorts_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_ResourceSorts_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_ResourceSorts_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResourceSorts_ResourceTypes_ResourceTypeId",
                        column: x => x.ResourceTypeId,
                        principalTable: "ResourceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceSorts_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceSorts_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    NormalizedTextSv = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, computedColumnSql: "CAST(CASE LOWER(JSON_VALUE([Metadata], '$.IsActive')) WHEN 'true' THEN 1 WHEN '1' THEN 1 WHEN 'false' THEN 0 WHEN '0' THEN 0 ELSE 1 END AS bit)", stored: true),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false, computedColumnSql: "COALESCE(TRY_CONVERT(int, JSON_VALUE([Metadata], '$.Type')), 0)", stored: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true, computedColumnSql: "CAST(NULLIF(LTRIM(RTRIM(JSON_VALUE([Metadata], '$.Note'))), '') AS nvarchar(3000))", stored: true),
                    ProductionNote = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true, computedColumnSql: "CAST(NULLIF(LTRIM(RTRIM(JSON_VALUE([Metadata], '$.Code'))), '') AS nvarchar(20))", stored: true),
                    IsOH = table.Column<bool>(type: "bit", nullable: false, computedColumnSql: "CAST(CASE LOWER(JSON_VALUE([Metadata], '$.IsOH')) WHEN 'true' THEN 1 WHEN '1' THEN 1 WHEN 'false' THEN 0 WHEN '0' THEN 0 ELSE 0 END AS bit)", stored: true),
                    ParentTaskId = table.Column<int>(type: "int", nullable: true),
                    OpportunityId = table.Column<int>(type: "int", nullable: true),
                    CalculationId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.CheckConstraint("CK_Tasks_Calculation_Positive", "[CalculationId] > 0");
                    table.CheckConstraint("CK_Tasks_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_Tasks_Quantity_NonNegative", "[Quantity] IS NULL OR [Quantity] >= 0");
                    table.CheckConstraint("CK_Tasks_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_Tasks_Calculations_CalculationId",
                        column: x => x.CalculationId,
                        principalTable: "Calculations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tasks_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "Opportunities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_TaskStatuses_StatusId",
                        column: x => x.StatusId,
                        principalTable: "TaskStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_Tasks_ParentTaskId",
                        column: x => x.ParentTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tasks_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tasks_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenderDeadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenderQA = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectTypeId = table.Column<int>(type: "int", nullable: true),
                    ProjectStatusId = table.Column<int>(type: "int", nullable: true),
                    FolderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: true),
                    ProcurementMethodId = table.Column<int>(type: "int", nullable: true),
                    ProcurementProcedureId = table.Column<int>(type: "int", nullable: true),
                    CompensationId = table.Column<int>(type: "int", nullable: true),
                    ContractId = table.Column<int>(type: "int", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    BidEvaluationModel = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.CheckConstraint("CK_Projects_DateRange", "[EndDate] >= [StartDate]");
                    table.CheckConstraint("CK_Projects_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_Projects_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_Projects_Compensations_CompensationId",
                        column: x => x.CompensationId,
                        principalTable: "Compensations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalTable: "Contracts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_Folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Projects_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_ProcurementMethods_ProcurementMethodId",
                        column: x => x.ProcurementMethodId,
                        principalTable: "ProcurementMethods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_ProcurementProcedures_ProcurementProcedureId",
                        column: x => x.ProcurementProcedureId,
                        principalTable: "ProcurementProcedures",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_ProjectStatuses_ProjectStatusId",
                        column: x => x.ProjectStatusId,
                        principalTable: "ProjectStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_ProjectTypes_ProjectTypeId",
                        column: x => x.ProjectTypeId,
                        principalTable: "ProjectTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tenders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Attributes = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    CalculationId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tenders_Calculations_CalculationId",
                        column: x => x.CalculationId,
                        principalTable: "Calculations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tenders_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tenders_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tenders_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ResType = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true, computedColumnSql: "CAST(NULLIF(LTRIM(RTRIM(JSON_VALUE([Metadata], '$.Note'))), '') AS nvarchar(3000))", stored: true),
                    ProductionNote = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    OpportunityId = table.Column<int>(type: "int", nullable: true),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    ResourceSortId = table.Column<int>(type: "int", nullable: true),
                    ResourceTypeId = table.Column<int>(type: "int", nullable: true),
                    PrimaryOfferId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                    table.CheckConstraint("CK_Resources_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_Resources_Quantity_NonNegative", "[Quantity] IS NULL OR [Quantity] >= 0");
                    table.CheckConstraint("CK_Resources_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.CheckConstraint("CK_Resources_Task_Positive", "[TaskId] > 0");
                    table.ForeignKey(
                        name: "FK_Resources_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resources_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "Opportunities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resources_ResourceSorts_ResourceSortId",
                        column: x => x.ResourceSortId,
                        principalTable: "ResourceSorts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resources_ResourceTypes_ResourceTypeId",
                        column: x => x.ResourceTypeId,
                        principalTable: "ResourceTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resources_StatusResources_StatusId",
                        column: x => x.StatusId,
                        principalTable: "StatusResources",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resources_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Resources_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Resources_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectBidPriceColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PartType = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectBidPriceColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectBidPriceColumns_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectBidPriceColumns_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectBidPriceColumns_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectBids",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BidderName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PricesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeductionPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    IsWinner = table.Column<bool>(type: "bit", nullable: false),
                    IsAwarded = table.Column<bool>(type: "bit", nullable: false),
                    Placement = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectBids", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectBids_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectBids_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectBids_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedWithUserId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    Role = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectShares", x => x.Id);
                    table.CheckConstraint("CK_ProjectShares_OneRecipient", "([SharedWithUserId] IS NOT NULL AND [DepartmentId] IS NULL) OR ([SharedWithUserId] IS NULL AND [DepartmentId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ProjectShares_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectShares_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectShares_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectShares_Users_SharedWithUserId",
                        column: x => x.SharedWithUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectShares_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TenderAttributeBinds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenderId = table.Column<int>(type: "int", nullable: false),
                    TenderAttributeId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderAttributeBinds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenderAttributeBinds_TenderAttributeDefinitions_TenderAttributeId",
                        column: x => x.TenderAttributeId,
                        principalTable: "TenderAttributeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenderAttributeBinds_Tenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "Tenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TenderAttributeBinds_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TenderAttributeBinds_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: true),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    BaseCostValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true, computedColumnSql: "TRY_CONVERT(decimal(18,2), JSON_VALUE([Metadata], '$.BaseCost'))", stored: true),
                    CostValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true, computedColumnSql: "TRY_CONVERT(decimal(18,2), JSON_VALUE([Metadata], '$.Cost'))", stored: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offers_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Offers_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Offers_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Offers_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProjectShareCalculations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectShareId = table.Column<int>(type: "int", nullable: false),
                    CalculationId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectShareCalculations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectShareCalculations_Calculations_CalculationId",
                        column: x => x.CalculationId,
                        principalTable: "Calculations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectShareCalculations_ProjectShares_ProjectShareId",
                        column: x => x.ProjectShareId,
                        principalTable: "ProjectShares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountGroups_CreatedBy",
                table: "AccountGroups",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AccountGroups_UpdatedBy",
                table: "AccountGroups",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_AccountGroups_Tenant_Name",
                table: "AccountGroups",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountGroupId",
                table: "Accounts",
                column: "AccountGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_CreatedBy",
                table: "Accounts",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Tenant_Group_Visible_Id",
                table: "Accounts",
                columns: new[] { "TenantId", "AccountGroupId", "IsVisible", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Tenant_Name",
                table: "Accounts",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UpdatedBy",
                table: "Accounts",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_Accounts_Tenant_Code",
                table: "Accounts",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Applications_DepartmentId",
                table: "Applications",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_Tenant_Department_Visible_Id",
                table: "Applications",
                columns: new[] { "TenantId", "DepartmentId", "IsVisible", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_Tenant_User",
                table: "Applications",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationValues_ApplicationId",
                table: "ApplicationValues",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationValues_CalculationId",
                table: "ApplicationValues",
                column: "CalculationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationValues_Tenant_App",
                table: "ApplicationValues",
                columns: new[] { "TenantId", "ApplicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationValues_Tenant_Calc_LastUpdate",
                table: "ApplicationValues",
                columns: new[] { "TenantId", "CalculationId", "LastUpdate" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationValues_Tenant_User",
                table: "ApplicationValues",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "UX_ApplicationValues_Tenant_Calc_App",
                table: "ApplicationValues",
                columns: new[] { "TenantId", "CalculationId", "ApplicationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_CompensationId",
                table: "Calculations",
                column: "CompensationId");

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_ContractId",
                table: "Calculations",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_CreatedBy",
                table: "Calculations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_DeletedBy",
                table: "Calculations",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_DepartmentId",
                table: "Calculations",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_OrganisationId",
                table: "Calculations",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_ProcurementMethodsId",
                table: "Calculations",
                column: "ProcurementMethodsId");

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_ProjectId",
                table: "Calculations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_StatusId",
                table: "Calculations",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_TemplateColumnId",
                table: "Calculations",
                column: "TemplateColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_TemplateId",
                table: "Calculations",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_Tenant_CreatedBy",
                table: "Calculations",
                columns: new[] { "TenantId", "CreatedBy" });

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_Tenant_Department",
                table: "Calculations",
                columns: new[] { "TenantId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_Tenant_Id_Department",
                table: "Calculations",
                columns: new[] { "TenantId", "Id", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_Tenant_Project",
                table: "Calculations",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_Tenant_Project_Department_Order",
                table: "Calculations",
                columns: new[] { "TenantId", "ProjectId", "DepartmentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_Tenant_Project_Private_Order",
                table: "Calculations",
                columns: new[] { "TenantId", "ProjectId", "IsPrivate", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_Tenant_Project_VersionGroup_Current",
                table: "Calculations",
                columns: new[] { "TenantId", "ProjectId", "VersionGroupId", "IsCurrentVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_Tenant_Project_VersionGroup_Number",
                table: "Calculations",
                columns: new[] { "TenantId", "ProjectId", "VersionGroupId", "VersionNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_Tenant_Status",
                table: "Calculations",
                columns: new[] { "TenantId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_TypeId",
                table: "Calculations",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_UpdatedBy",
                table: "Calculations",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_Calculations_Tenant_Project_Code",
                table: "Calculations",
                columns: new[] { "TenantId", "ProjectId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Compensations_CreatedBy",
                table: "Compensations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Compensations_Tenant_Name",
                table: "Compensations",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Compensations_Tenant_Visible_Order",
                table: "Compensations",
                columns: new[] { "TenantId", "IsVisible", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Compensations_UpdatedBy",
                table: "Compensations",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_CreatedBy",
                table: "Contracts",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_Tenant_Name",
                table: "Contracts",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_Tenant_Visible_Order",
                table: "Contracts",
                columns: new[] { "TenantId", "IsVisible", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_UpdatedBy",
                table: "Contracts",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CreatedBy",
                table: "Departments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_UpdatedBy",
                table: "Departments",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_Departments_Tenant_Name",
                table: "Departments",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Folders_CreatedBy",
                table: "Folders",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_DepartmentId",
                table: "Folders",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_Tenant_Department_Name",
                table: "Folders",
                columns: new[] { "TenantId", "DepartmentId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Folders_Tenant_Department_Visible_Order",
                table: "Folders",
                columns: new[] { "TenantId", "DepartmentId", "IsVisible", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Folders_UpdatedBy",
                table: "Folders",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_CreatedBy",
                table: "Offers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_OrganisationId",
                table: "Offers",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ResourceId",
                table: "Offers",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_Tenant_BaseCostValue",
                table: "Offers",
                columns: new[] { "TenantId", "BaseCostValue" });

            migrationBuilder.CreateIndex(
                name: "IX_Offers_Tenant_CostValue",
                table: "Offers",
                columns: new[] { "TenantId", "CostValue" });

            migrationBuilder.CreateIndex(
                name: "IX_Offers_Tenant_Org_Date",
                table: "Offers",
                columns: new[] { "TenantId", "OrganisationId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Offers_TenantId_ResourceId",
                table: "Offers",
                columns: new[] { "TenantId", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Offers_UpdatedBy",
                table: "Offers",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_CalculationId",
                table: "Opportunities",
                column: "CalculationId");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_CreatedBy",
                table: "Opportunities",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_Tenant_Calc",
                table: "Opportunities",
                columns: new[] { "TenantId", "CalculationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_UpdatedBy",
                table: "Opportunities",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationCategories_CreatedBy",
                table: "OrganisationCategories",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationCategories_ParentCategoryId",
                table: "OrganisationCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationCategories_Tenant_Parent",
                table: "OrganisationCategories",
                columns: new[] { "TenantId", "ParentCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationCategories_UpdatedBy",
                table: "OrganisationCategories",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_OrganisationCategories_Tenant_Name",
                table: "OrganisationCategories",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_CreatedBy",
                table: "Organisations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_OrganisationCategoryId",
                table: "Organisations",
                column: "OrganisationCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_OrganisationTypeId",
                table: "Organisations",
                column: "OrganisationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_Tenant_Category",
                table: "Organisations",
                columns: new[] { "TenantId", "OrganisationCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_Tenant_Visible_Name",
                table: "Organisations",
                columns: new[] { "TenantId", "IsVisible", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Organisations_UpdatedBy",
                table: "Organisations",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationTypes_CreatedBy",
                table: "OrganisationTypes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationTypes_UpdatedBy",
                table: "OrganisationTypes",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_OrganisationTypes_Tenant_Name",
                table: "OrganisationTypes",
                columns: new[] { "TenantId", "Name" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementMethods_CreatedBy",
                table: "ProcurementMethods",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementMethods_Tenant_Name",
                table: "ProcurementMethods",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementMethods_Tenant_Visible_Order",
                table: "ProcurementMethods",
                columns: new[] { "TenantId", "IsVisible", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementMethods_UpdatedBy",
                table: "ProcurementMethods",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementProcedures_CreatedBy",
                table: "ProcurementProcedures",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementProcedures_Tenant_Name",
                table: "ProcurementProcedures",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementProcedures_Tenant_Visible_Order",
                table: "ProcurementProcedures",
                columns: new[] { "TenantId", "IsVisible", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementProcedures_UpdatedBy",
                table: "ProcurementProcedures",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBidPriceColumns_CreatedBy",
                table: "ProjectBidPriceColumns",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBidPriceColumns_ProjectId",
                table: "ProjectBidPriceColumns",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBidPriceColumns_Tenant_Project",
                table: "ProjectBidPriceColumns",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBidPriceColumns_UpdatedBy",
                table: "ProjectBidPriceColumns",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBids_CreatedBy",
                table: "ProjectBids",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBids_ProjectId",
                table: "ProjectBids",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBids_Tenant_Project",
                table: "ProjectBids",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectBids_UpdatedBy",
                table: "ProjectBids",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CompensationId",
                table: "Projects",
                column: "CompensationId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ContractId",
                table: "Projects",
                column: "ContractId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_CreatedBy",
                table: "Projects",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_DeletedBy",
                table: "Projects",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_FolderId",
                table: "Projects",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OrganisationId",
                table: "Projects",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProcurementMethodId",
                table: "Projects",
                column: "ProcurementMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProcurementProcedureId",
                table: "Projects",
                column: "ProcurementProcedureId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectStatusId",
                table: "Projects",
                column: "ProjectStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectTypeId",
                table: "Projects",
                column: "ProjectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Tenant_CreatedBy",
                table: "Projects",
                columns: new[] { "TenantId", "CreatedBy" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Tenant_Folder_Visible_Order",
                table: "Projects",
                columns: new[] { "TenantId", "FolderId", "IsArchived", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Tenant_Name",
                table: "Projects",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_UpdatedBy",
                table: "Projects",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_Projects_Tenant_Code",
                table: "Projects",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [Code] IS NOT NULL AND [Code] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectShareCalculations_CalculationId",
                table: "ProjectShareCalculations",
                column: "CalculationId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectShareCalculations_ProjectShareId",
                table: "ProjectShareCalculations",
                column: "ProjectShareId");

            migrationBuilder.CreateIndex(
                name: "UX_ProjectShareCalculations_Share_Calc",
                table: "ProjectShareCalculations",
                columns: new[] { "TenantId", "ProjectShareId", "CalculationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectShares_CreatedBy",
                table: "ProjectShares",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectShares_DepartmentId",
                table: "ProjectShares",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectShares_ProjectId",
                table: "ProjectShares",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectShares_SharedWithUserId",
                table: "ProjectShares",
                column: "SharedWithUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectShares_Tenant_Project",
                table: "ProjectShares",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectShares_UpdatedBy",
                table: "ProjectShares",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_ProjectShares_Project_Department",
                table: "ProjectShares",
                columns: new[] { "TenantId", "ProjectId", "DepartmentId" },
                unique: true,
                filter: "[DepartmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ProjectShares_Project_User",
                table: "ProjectShares",
                columns: new[] { "TenantId", "ProjectId", "SharedWithUserId" },
                unique: true,
                filter: "[SharedWithUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStatuses_CreatedBy",
                table: "ProjectStatuses",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStatuses_Tenant_Name",
                table: "ProjectStatuses",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStatuses_Tenant_Visible_Order",
                table: "ProjectStatuses",
                columns: new[] { "TenantId", "IsVisible", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectStatuses_UpdatedBy",
                table: "ProjectStatuses",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTypes_CreatedBy",
                table: "ProjectTypes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTypes_Tenant_Name",
                table: "ProjectTypes",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTypes_Tenant_Visible_Order",
                table: "ProjectTypes",
                columns: new[] { "TenantId", "IsVisible", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTypes_UpdatedBy",
                table: "ProjectTypes",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_AccountId",
                table: "Resources",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_CreatedBy",
                table: "Resources",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_OpportunityId",
                table: "Resources",
                column: "OpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_ResourceSortId",
                table: "Resources",
                column: "ResourceSortId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_ResourceTypeId",
                table: "Resources",
                column: "ResourceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_StatusId",
                table: "Resources",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_TaskId",
                table: "Resources",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Tenant_Account",
                table: "Resources",
                columns: new[] { "TenantId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Tenant_Opportunity",
                table: "Resources",
                columns: new[] { "TenantId", "OpportunityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Tenant_PrimaryOffer",
                table: "Resources",
                columns: new[] { "TenantId", "PrimaryOfferId" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Tenant_ResourceSort",
                table: "Resources",
                columns: new[] { "TenantId", "ResourceSortId" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Tenant_ResourceType",
                table: "Resources",
                columns: new[] { "TenantId", "ResourceTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Tenant_Status",
                table: "Resources",
                columns: new[] { "TenantId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Tenant_Task_Sort",
                table: "Resources",
                columns: new[] { "TenantId", "TaskId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_TenantId_TaskId",
                table: "Resources",
                columns: new[] { "TenantId", "TaskId" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_UpdatedBy",
                table: "Resources",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceSorts_AccountId",
                table: "ResourceSorts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceSorts_CreatedBy",
                table: "ResourceSorts",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceSorts_ResourceTypeId",
                table: "ResourceSorts",
                column: "ResourceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceSorts_Tenant_Type_Name",
                table: "ResourceSorts",
                columns: new[] { "TenantId", "ResourceTypeId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceSorts_Tenant_Type_Visible_Order",
                table: "ResourceSorts",
                columns: new[] { "TenantId", "ResourceTypeId", "IsVisible", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceSorts_TenantId_ResourceTypeId",
                table: "ResourceSorts",
                columns: new[] { "TenantId", "ResourceTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceSorts_UpdatedBy",
                table: "ResourceSorts",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTypes_AccountId",
                table: "ResourceTypes",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTypes_CreatedBy",
                table: "ResourceTypes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTypes_Tenant_Kind",
                table: "ResourceTypes",
                columns: new[] { "TenantId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTypes_Tenant_Kind_Visible_Order",
                table: "ResourceTypes",
                columns: new[] { "TenantId", "Kind", "IsVisible", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTypes_Tenant_Name",
                table: "ResourceTypes",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTypes_TenantId_AccountId",
                table: "ResourceTypes",
                columns: new[] { "TenantId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTypes_UpdatedBy",
                table: "ResourceTypes",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ShareCalcs_CalculationId",
                table: "ShareCalcs",
                column: "CalculationId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareCalcs_CreatedAtUserId",
                table: "ShareCalcs",
                column: "CreatedAtUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareCalcs_CreatedBy",
                table: "ShareCalcs",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ShareCalcs_DepartmentId",
                table: "ShareCalcs",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareCalcs_Tenant_Calc",
                table: "ShareCalcs",
                columns: new[] { "TenantId", "CalculationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShareCalcs_Tenant_Department",
                table: "ShareCalcs",
                columns: new[] { "TenantId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ShareCalcs_UpdatedBy",
                table: "ShareCalcs",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_ShareCalcs_Tenant_Calc_Department",
                table: "ShareCalcs",
                columns: new[] { "TenantId", "CalculationId", "DepartmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Statuses_CreatedBy",
                table: "Statuses",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Statuses_Tenant_Name",
                table: "Statuses",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Statuses_Tenant_Visible_Order",
                table: "Statuses",
                columns: new[] { "TenantId", "IsVisible", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Statuses_UpdatedBy",
                table: "Statuses",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StatusResources_CreatedBy",
                table: "StatusResources",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StatusResources_Tenant_Code",
                table: "StatusResources",
                columns: new[] { "TenantId", "Code" },
                filter: "[Code] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_StatusResources_Tenant_Name",
                table: "StatusResources",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatusResources_Tenant_Visible_Order",
                table: "StatusResources",
                columns: new[] { "TenantId", "IsVisible", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_StatusResources_UpdatedBy",
                table: "StatusResources",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Storages_CreatedBy",
                table: "Storages",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Storages_DepartmentId",
                table: "Storages",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Storages_Tenant_Department_Type_Sort_Level",
                table: "Storages",
                columns: new[] { "TenantId", "DepartmentId", "StorageType", "StorageSort", "StorageLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_Storages_UpdatedBy",
                table: "Storages",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CalculationId",
                table: "Tasks",
                column: "CalculationId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CreatedBy",
                table: "Tasks",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_OpportunityId",
                table: "Tasks",
                column: "OpportunityId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ParentTaskId",
                table: "Tasks",
                column: "ParentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_StatusId",
                table: "Tasks",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Tenant_Calc_Parent_Sort",
                table: "Tasks",
                columns: new[] { "TenantId", "CalculationId", "ParentTaskId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Tenant_Calc_Status",
                table: "Tasks",
                columns: new[] { "TenantId", "CalculationId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Tenant_CreatedBy",
                table: "Tasks",
                columns: new[] { "TenantId", "CreatedBy" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Tenant_NormalizedTextSv",
                table: "Tasks",
                columns: new[] { "TenantId", "NormalizedTextSv" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Tenant_Parent",
                table: "Tasks",
                columns: new[] { "TenantId", "ParentTaskId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_TenantId_CalculationId",
                table: "Tasks",
                columns: new[] { "TenantId", "CalculationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UpdatedBy",
                table: "Tasks",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TaskStatuses_CreatedBy",
                table: "TaskStatuses",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TaskStatuses_Tenant_Code",
                table: "TaskStatuses",
                columns: new[] { "TenantId", "Code" },
                filter: "[Code] <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_TaskStatuses_Tenant_Name",
                table: "TaskStatuses",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskStatuses_Tenant_Visible_Order",
                table: "TaskStatuses",
                columns: new[] { "TenantId", "IsVisible", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskStatuses_UpdatedBy",
                table: "TaskStatuses",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateColumns_CreatedBy",
                table: "TemplateColumns",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateColumns_DepartmentId",
                table: "TemplateColumns",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateColumns_Tenant_Department_Id",
                table: "TemplateColumns",
                columns: new[] { "TenantId", "DepartmentId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateColumns_Tenant_Department_Name",
                table: "TemplateColumns",
                columns: new[] { "TenantId", "DepartmentId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_TemplateColumns_UpdatedBy",
                table: "TemplateColumns",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_CreatedBy",
                table: "Templates",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_DepartmentId",
                table: "Templates",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Templates_TenantId_DepartmentId_Id",
                table: "Templates",
                columns: new[] { "TenantId", "DepartmentId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Templates_TenantId_Name",
                table: "Templates",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Templates_UpdatedBy",
                table: "Templates",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TenderAttributeBinds_CreatedBy",
                table: "TenderAttributeBinds",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TenderAttributeBinds_TenderAttributeId",
                table: "TenderAttributeBinds",
                column: "TenderAttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderAttributeBinds_TenderId",
                table: "TenderAttributeBinds",
                column: "TenderId");

            migrationBuilder.CreateIndex(
                name: "IX_TenderAttributeBinds_UpdatedBy",
                table: "TenderAttributeBinds",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_TenderAttributeBinds_Tenant_Tender_Attr",
                table: "TenderAttributeBinds",
                columns: new[] { "TenantId", "TenderId", "TenderAttributeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenderAttrDefs_Tenant_Calc",
                table: "TenderAttributeDefinitions",
                columns: new[] { "TenantId", "CalculationId" });

            migrationBuilder.CreateIndex(
                name: "IX_TenderAttributeDefinitions_CalculationId",
                table: "TenderAttributeDefinitions",
                column: "CalculationId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_CalculationId",
                table: "Tenders",
                column: "CalculationId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_CreatedBy",
                table: "Tenders",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_OrganisationId",
                table: "Tenders",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_Tenant_Calc",
                table: "Tenders",
                columns: new[] { "TenantId", "CalculationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_Tenant_Org",
                table: "Tenders",
                columns: new[] { "TenantId", "OrganisationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_UpdatedBy",
                table: "Tenders",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserListSettings_CreatedBy",
                table: "UserListSettings",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_UserListSettings_UpdatedBy",
                table: "UserListSettings",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_UserListSettings_Tenant_User_Scope_Kind",
                table: "UserListSettings",
                columns: new[] { "TenantId", "UserId", "Scope", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserManagementAuditLogs_Tenant_CreatedAt",
                table: "UserManagementAuditLogs",
                columns: new[] { "TenantId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserManagementAuditLogs_Tenant_TargetUser",
                table: "UserManagementAuditLogs",
                columns: new[] { "TenantId", "TargetUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedBy",
                table: "Users",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Tenant_UserName",
                table: "Users",
                columns: new[] { "TenantId", "UserName" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_UpdatedBy",
                table: "Users",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_Users_Tenant_Email",
                table: "Users",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Users_Tenant_ExternalAuthId",
                table: "Users",
                columns: new[] { "TenantId", "ExternalAuthId" },
                unique: true,
                filter: "[ExternalAuthId] IS NOT NULL AND [ExternalAuthId] <> ''");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountGroups_Users_CreatedBy",
                table: "AccountGroups",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountGroups_Users_UpdatedBy",
                table: "AccountGroups",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Users_CreatedBy",
                table: "Accounts",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Users_UpdatedBy",
                table: "Accounts",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Departments_DepartmentId",
                table: "Applications",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationValues_Calculations_CalculationId",
                table: "ApplicationValues",
                column: "CalculationId",
                principalTable: "Calculations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_Compensations_CompensationId",
                table: "Calculations",
                column: "CompensationId",
                principalTable: "Compensations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_Contracts_ContractId",
                table: "Calculations",
                column: "ContractId",
                principalTable: "Contracts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_Departments_DepartmentId",
                table: "Calculations",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_Organisations_OrganisationId",
                table: "Calculations",
                column: "OrganisationId",
                principalTable: "Organisations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_ProcurementMethods_ProcurementMethodsId",
                table: "Calculations",
                column: "ProcurementMethodsId",
                principalTable: "ProcurementMethods",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_ProjectTypes_TypeId",
                table: "Calculations",
                column: "TypeId",
                principalTable: "ProjectTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_Projects_ProjectId",
                table: "Calculations",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_Statuses_StatusId",
                table: "Calculations",
                column: "StatusId",
                principalTable: "Statuses",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_TemplateColumns_TemplateColumnId",
                table: "Calculations",
                column: "TemplateColumnId",
                principalTable: "TemplateColumns",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_Templates_TemplateId",
                table: "Calculations",
                column: "TemplateId",
                principalTable: "Templates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_Users_CreatedBy",
                table: "Calculations",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_Users_DeletedBy",
                table: "Calculations",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_Users_UpdatedBy",
                table: "Calculations",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Compensations_Users_CreatedBy",
                table: "Compensations",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Compensations_Users_UpdatedBy",
                table: "Compensations",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Users_CreatedBy",
                table: "Contracts",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Users_UpdatedBy",
                table: "Contracts",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Users_CreatedBy",
                table: "Departments",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Users_UpdatedBy",
                table: "Departments",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Users_CreatedBy",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Users_UpdatedBy",
                table: "Departments");

            migrationBuilder.DropTable(
                name: "ApplicationValues");

            migrationBuilder.DropTable(
                name: "Offers");

            migrationBuilder.DropTable(
                name: "PriceImportCandidates");

            migrationBuilder.DropTable(
                name: "PriceImportMappings");

            migrationBuilder.DropTable(
                name: "PriceListItems");

            migrationBuilder.DropTable(
                name: "ProjectBidPriceColumns");

            migrationBuilder.DropTable(
                name: "ProjectBids");

            migrationBuilder.DropTable(
                name: "ProjectShareCalculations");

            migrationBuilder.DropTable(
                name: "ShareCalcs");

            migrationBuilder.DropTable(
                name: "Storages");

            migrationBuilder.DropTable(
                name: "TenderAttributeBinds");

            migrationBuilder.DropTable(
                name: "UserListSettings");

            migrationBuilder.DropTable(
                name: "UserManagementAuditLogs");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "PriceImportJobs");

            migrationBuilder.DropTable(
                name: "PriceLists");

            migrationBuilder.DropTable(
                name: "ProjectShares");

            migrationBuilder.DropTable(
                name: "TenderAttributeDefinitions");

            migrationBuilder.DropTable(
                name: "Tenders");

            migrationBuilder.DropTable(
                name: "ResourceSorts");

            migrationBuilder.DropTable(
                name: "StatusResources");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "ResourceTypes");

            migrationBuilder.DropTable(
                name: "Opportunities");

            migrationBuilder.DropTable(
                name: "TaskStatuses");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Calculations");

            migrationBuilder.DropTable(
                name: "AccountGroups");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Statuses");

            migrationBuilder.DropTable(
                name: "TemplateColumns");

            migrationBuilder.DropTable(
                name: "Templates");

            migrationBuilder.DropTable(
                name: "Compensations");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "Folders");

            migrationBuilder.DropTable(
                name: "Organisations");

            migrationBuilder.DropTable(
                name: "ProcurementMethods");

            migrationBuilder.DropTable(
                name: "ProcurementProcedures");

            migrationBuilder.DropTable(
                name: "ProjectStatuses");

            migrationBuilder.DropTable(
                name: "ProjectTypes");

            migrationBuilder.DropTable(
                name: "OrganisationCategories");

            migrationBuilder.DropTable(
                name: "OrganisationTypes");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropSequence(
                name: "OrderSeq");
        }
    }
}
