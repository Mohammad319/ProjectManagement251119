using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sharding_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateSequence<int>(
                name: "OrderSeq",
                startValue: 0L,
                incrementBy: 100);

            migrationBuilder.CreateTable(
                name: "AccountGroup",
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
                    table.PrimaryKey("PK_AccountGroup", x => x.Id);
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
                    table.ForeignKey(
                        name: "FK_Accounts_AccountGroup_AccountGroupId",
                        column: x => x.AccountGroupId,
                        principalTable: "AccountGroup",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    table.ForeignKey(
                        name: "FK_ApplicationValues_Applications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "Applications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttributeNameTender",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CalculationId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttributeNameTender", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CalcProjectType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR OrderSeq"),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalcProjectType", x => x.Id);
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
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Tax = table.Column<double>(type: "float", nullable: false),
                    Procurement = table.Column<int>(type: "int", nullable: false),
                    TenderDeadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenderQA = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SortOrder = table.Column<double>(type: "float", nullable: false),
                    PublicationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsPrivate = table.Column<bool>(type: "bit", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: true),
                    TypeId = table.Column<int>(type: "int", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    ProcurementMethodsId = table.Column<int>(type: "int", nullable: true),
                    CompensationId = table.Column<int>(type: "int", nullable: true),
                    ContractId = table.Column<int>(type: "int", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<int>(type: "int", nullable: true),
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
                    table.CheckConstraint("CK_Calculations_Tax_Range", "[Tax] >= 0 AND [Tax] <= 100");
                    table.ForeignKey(
                        name: "FK_Calculations_CalcProjectType_TypeId",
                        column: x => x.TypeId,
                        principalTable: "CalcProjectType",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Opportunity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OpportunitiesRisks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OpportunityType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CalculationId = table.Column<int>(type: "int", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Opportunity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Opportunity_Calculations_CalculationId",
                        column: x => x.CalculationId,
                        principalTable: "Calculations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CalculationStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR OrderSeq"),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculationStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Compensations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR OrderSeq"),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Compensations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR OrderSeq"),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Department",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Department", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExternalAuthId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
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
                    table.ForeignKey(
                        name: "FK_Users_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "dbo",
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
                    SortOrder = table.Column<double>(type: "float", nullable: false),
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
                    table.ForeignKey(
                        name: "FK_Folders_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Folders_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Folders_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganisationCategory",
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
                    table.PrimaryKey("PK_OrganisationCategory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganisationCategory_OrganisationCategory_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "OrganisationCategory",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrganisationCategory_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganisationCategory_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrganisationType",
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
                    table.PrimaryKey("PK_OrganisationType", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganisationType_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganisationType_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProcurementMethod",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR OrderSeq"),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcurementMethod", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcurementMethod_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProcurementMethod_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResourceStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR OrderSeq"),
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
                    table.PrimaryKey("PK_ResourceStatus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceStatus_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceStatus_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "dbo",
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
                    table.ForeignKey(
                        name: "FK_ResourceTypes_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ResourceTypes_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceTypes_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShareCalc",
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
                    table.PrimaryKey("PK_ShareCalc", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareCalc_Calculations_CalculationId",
                        column: x => x.CalculationId,
                        principalTable: "Calculations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShareCalc_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShareCalc_Users_CreatedAtUserId",
                        column: x => x.CreatedAtUserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShareCalc_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShareCalc_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "dbo",
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
                    StorageValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    table.ForeignKey(
                        name: "FK_Storages_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Storages_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Storages_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValueSql: "NEXT VALUE FOR OrderSeq"),
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
                    table.PrimaryKey("PK_TaskStatus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskStatus_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskStatus_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "dbo",
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
                        name: "FK_Templates_Department_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Department",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Templates_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Templates_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Organisation",
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
                    table.PrimaryKey("PK_Organisation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organisation_OrganisationCategory_OrganisationCategoryId",
                        column: x => x.OrganisationCategoryId,
                        principalTable: "OrganisationCategory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Organisation_OrganisationType_OrganisationTypeId",
                        column: x => x.OrganisationTypeId,
                        principalTable: "OrganisationType",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Organisation_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Organisation_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "dbo",
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
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceSorts_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "dbo",
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
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<double>(type: "float", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    IsOH = table.Column<bool>(type: "bit", nullable: false),
                    ParentTaskId = table.Column<int>(type: "int", nullable: true),
                    OpportunityId = table.Column<int>(type: "int", nullable: true),
                    CalculationId = table.Column<int>(type: "int", nullable: false),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasks_Calculations_CalculationId",
                        column: x => x.CalculationId,
                        principalTable: "Calculations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tasks_Opportunity_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "Opportunity",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_TaskStatus_StatusId",
                        column: x => x.StatusId,
                        principalTable: "TaskStatus",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_Tasks_ParentTaskId",
                        column: x => x.ParentTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenderDeadline = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TenderQA = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SortOrder = table.Column<double>(type: "float", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectTypeId = table.Column<int>(type: "int", nullable: true),
                    FolderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: true),
                    ProcurementMethodId = table.Column<int>(type: "int", nullable: true),
                    CompensationId = table.Column<int>(type: "int", nullable: true),
                    ContractId = table.Column<int>(type: "int", nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    DepartmentEntityId = table.Column<int>(type: "int", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_Projects_CalcProjectType_ProjectTypeId",
                        column: x => x.ProjectTypeId,
                        principalTable: "CalcProjectType",
                        principalColumn: "Id");
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
                        name: "FK_Projects_Department_DepartmentEntityId",
                        column: x => x.DepartmentEntityId,
                        principalTable: "Department",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_Folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Folders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Projects_Organisation_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisation",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_ProcurementMethod_ProcurementMethodId",
                        column: x => x.ProcurementMethodId,
                        principalTable: "ProcurementMethod",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Projects_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_Users_DeletedBy",
                        column: x => x.DeletedBy,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Projects_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalSchema: "dbo",
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
                    Attributes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CalculationId = table.Column<int>(type: "int", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
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
                        name: "FK_Tenders_Organisation_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisation",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    ResType = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<double>(type: "float", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    OpportunityId = table.Column<int>(type: "int", nullable: true),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    ResourceSortId = table.Column<int>(type: "int", nullable: true),
                    ResourceTypeId = table.Column<int>(type: "int", nullable: true),
                    PrimaryOfferId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resources_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resources_Opportunity_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "Opportunity",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resources_ResourceSorts_ResourceSortId",
                        column: x => x.ResourceSortId,
                        principalTable: "ResourceSorts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resources_ResourceStatus_StatusId",
                        column: x => x.StatusId,
                        principalTable: "ResourceStatus",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resources_ResourceTypes_ResourceTypeId",
                        column: x => x.ResourceTypeId,
                        principalTable: "ResourceTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Resources_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenderAttributeBind",
                columns: table => new
                {
                    TenderId = table.Column<int>(type: "int", nullable: false),
                    TenderAttributeId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenderAttributeBind", x => new { x.TenderAttributeId, x.TenderId });
                    table.ForeignKey(
                        name: "FK_TenderAttributeBind_AttributeNameTender_TenderAttributeId",
                        column: x => x.TenderAttributeId,
                        principalTable: "AttributeNameTender",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TenderAttributeBind_Tenders_TenderId",
                        column: x => x.TenderId,
                        principalTable: "Tenders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OrganisationId = table.Column<int>(type: "int", nullable: true),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    CalculationEntityId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offers_Calculations_CalculationEntityId",
                        column: x => x.CalculationEntityId,
                        principalTable: "Calculations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Offers_Organisation_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisation",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Offers_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountGroup_CreatedBy",
                table: "AccountGroup",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AccountGroup_TenantId_Id",
                table: "AccountGroup",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountGroup_UpdatedBy",
                table: "AccountGroup",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_AccountGroup_Tenant_Name",
                table: "AccountGroup",
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
                name: "IX_Accounts_Tenant_Name",
                table: "Accounts",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_TenantId_Id",
                table: "Accounts",
                columns: new[] { "TenantId", "Id" });

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
                name: "IX_ApplicationValues_ApplicationId",
                table: "ApplicationValues",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationValues_CalculationId",
                table: "ApplicationValues",
                column: "CalculationId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeNameTender_CalculationId",
                table: "AttributeNameTender",
                column: "CalculationId");

            migrationBuilder.CreateIndex(
                name: "IX_AttributeNameTender_TenantId_CalculationId",
                table: "AttributeNameTender",
                columns: new[] { "TenantId", "CalculationId" });

            migrationBuilder.CreateIndex(
                name: "IX_AttributeNameTender_TenantId_Id",
                table: "AttributeNameTender",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CalcProjectType_CreatedBy",
                table: "CalcProjectType",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CalcProjectType_TenantId_Id",
                table: "CalcProjectType",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CalcProjectType_UpdatedBy",
                table: "CalcProjectType",
                column: "UpdatedBy");

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
                name: "IX_Calculations_TemplateId",
                table: "Calculations",
                column: "TemplateId");

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
                name: "IX_Calculations_Tenant_Status",
                table: "Calculations",
                columns: new[] { "TenantId", "StatusId" });

            migrationBuilder.CreateIndex(
                name: "IX_Calculations_TenantId_Id",
                table: "Calculations",
                columns: new[] { "TenantId", "Id" });

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
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CalculationStatus_CreatedBy",
                table: "CalculationStatus",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CalculationStatus_TenantId_Id",
                table: "CalculationStatus",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_CalculationStatus_UpdatedBy",
                table: "CalculationStatus",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Compensations_CreatedBy",
                table: "Compensations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Compensations_TenantId_Id",
                table: "Compensations",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Compensations_UpdatedBy",
                table: "Compensations",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_CreatedBy",
                table: "Contracts",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_TenantId_Id",
                table: "Contracts",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Contracts_UpdatedBy",
                table: "Contracts",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Department_CreatedBy",
                table: "Department",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Department_TenantId_Id",
                table: "Department",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Department_UpdatedBy",
                table: "Department",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_Departments_Tenant_Name",
                table: "Department",
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
                name: "IX_Folders_TenantId_DepartmentId_IsVisible_SortOrder",
                table: "Folders",
                columns: new[] { "TenantId", "DepartmentId", "IsVisible", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Folders_TenantId_Id",
                table: "Folders",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Folders_UpdatedBy",
                table: "Folders",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_Folders_Tenant_Department_Name",
                table: "Folders",
                columns: new[] { "TenantId", "DepartmentId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_CalculationEntityId",
                table: "Offers",
                column: "CalculationEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_OrganisationId",
                table: "Offers",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ResourceId",
                table: "Offers",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_Tenant_Org_Date",
                table: "Offers",
                columns: new[] { "TenantId", "OrganisationId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Offers_TenantId_Id",
                table: "Offers",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Offers_TenantId_ResourceId",
                table: "Offers",
                columns: new[] { "TenantId", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Opportunity_CalculationId",
                table: "Opportunity",
                column: "CalculationId");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunity_TenantId_Id",
                table: "Opportunity",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Organisation_CreatedBy",
                table: "Organisation",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Organisation_OrganisationCategoryId",
                table: "Organisation",
                column: "OrganisationCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Organisation_OrganisationTypeId",
                table: "Organisation",
                column: "OrganisationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Organisation_TenantId_Id",
                table: "Organisation",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Organisation_UpdatedBy",
                table: "Organisation",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationCategory_CreatedBy",
                table: "OrganisationCategory",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationCategory_ParentCategoryId",
                table: "OrganisationCategory",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationCategory_TenantId_Id",
                table: "OrganisationCategory",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationCategory_UpdatedBy",
                table: "OrganisationCategory",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationType_CreatedBy",
                table: "OrganisationType",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationType_TenantId_Id",
                table: "OrganisationType",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganisationType_UpdatedBy",
                table: "OrganisationType",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementMethod_CreatedBy",
                table: "ProcurementMethod",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementMethod_TenantId_Id",
                table: "ProcurementMethod",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcurementMethod_UpdatedBy",
                table: "ProcurementMethod",
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
                name: "IX_Projects_DepartmentEntityId",
                table: "Projects",
                column: "DepartmentEntityId");

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
                name: "IX_Projects_ProjectTypeId",
                table: "Projects",
                column: "ProjectTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Tenant_CreatedBy",
                table: "Projects",
                columns: new[] { "TenantId", "CreatedBy" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Tenant_Department",
                table: "Projects",
                columns: new[] { "TenantId", "DepartmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Tenant_Folder_Visible_Order",
                table: "Projects",
                columns: new[] { "TenantId", "FolderId", "IsVisible", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_TenantId_Id",
                table: "Projects",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_UpdatedBy",
                table: "Projects",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "UX_Projects_Tenant_Code",
                table: "Projects",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_AccountId",
                table: "Resources",
                column: "AccountId");

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
                name: "IX_Resources_TenantId_Id",
                table: "Resources",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_TenantId_TaskId",
                table: "Resources",
                columns: new[] { "TenantId", "TaskId" });

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
                name: "IX_ResourceSorts_TenantId_Id",
                table: "ResourceSorts",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceSorts_TenantId_ResourceTypeId",
                table: "ResourceSorts",
                columns: new[] { "TenantId", "ResourceTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceSorts_UpdatedBy",
                table: "ResourceSorts",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceStatus_CreatedBy",
                table: "ResourceStatus",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceStatus_TenantId_Id",
                table: "ResourceStatus",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceStatus_UpdatedBy",
                table: "ResourceStatus",
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
                name: "IX_ResourceTypes_TenantId_AccountId",
                table: "ResourceTypes",
                columns: new[] { "TenantId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTypes_TenantId_Id",
                table: "ResourceTypes",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTypes_UpdatedBy",
                table: "ResourceTypes",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ShareCalc_CalculationId",
                table: "ShareCalc",
                column: "CalculationId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareCalc_CreatedAtUserId",
                table: "ShareCalc",
                column: "CreatedAtUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareCalc_CreatedBy",
                table: "ShareCalc",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ShareCalc_DepartmentId",
                table: "ShareCalc",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShareCalc_TenantId_Id",
                table: "ShareCalc",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ShareCalc_UpdatedBy",
                table: "ShareCalc",
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
                name: "IX_Storages_TenantId_Id",
                table: "Storages",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Storages_UpdatedBy",
                table: "Storages",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CalculationId",
                table: "Tasks",
                column: "CalculationId");

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
                name: "IX_Tasks_TenantId_CalculationId",
                table: "Tasks",
                columns: new[] { "TenantId", "CalculationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_TenantId_Id",
                table: "Tasks",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskStatus_CreatedBy",
                table: "TaskStatus",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TaskStatus_TenantId_Id",
                table: "TaskStatus",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskStatus_UpdatedBy",
                table: "TaskStatus",
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
                name: "IX_Templates_TenantId_Id",
                table: "Templates",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Templates_TenantId_Name",
                table: "Templates",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Templates_UpdatedBy",
                table: "Templates",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TenderAttributeBind_TenantId_Id",
                table: "TenderAttributeBind",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_TenderAttributeBind_TenderId",
                table: "TenderAttributeBind",
                column: "TenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_CalculationId",
                table: "Tenders",
                column: "CalculationId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_OrganisationId",
                table: "Tenders",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_TenantId_Id",
                table: "Tenders",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedBy",
                schema: "dbo",
                table: "Users",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DepartmentId",
                schema: "dbo",
                table: "Users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                schema: "dbo",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Id",
                schema: "dbo",
                table: "Users",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_UpdatedBy",
                schema: "dbo",
                table: "Users",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_AccountGroup_Users_CreatedBy",
                table: "AccountGroup",
                column: "CreatedBy",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountGroup_Users_UpdatedBy",
                table: "AccountGroup",
                column: "UpdatedBy",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Users_CreatedBy",
                table: "Accounts",
                column: "CreatedBy",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_Users_UpdatedBy",
                table: "Accounts",
                column: "UpdatedBy",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Department_DepartmentId",
                table: "Applications",
                column: "DepartmentId",
                principalTable: "Department",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationValues_Calculations_CalculationId",
                table: "ApplicationValues",
                column: "CalculationId",
                principalTable: "Calculations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AttributeNameTender_Calculations_CalculationId",
                table: "AttributeNameTender",
                column: "CalculationId",
                principalTable: "Calculations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CalcProjectType_Users_CreatedBy",
                table: "CalcProjectType",
                column: "CreatedBy",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CalcProjectType_Users_UpdatedBy",
                table: "CalcProjectType",
                column: "UpdatedBy",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_CalculationStatus_StatusId",
                table: "Calculations",
                column: "StatusId",
                principalTable: "CalculationStatus",
                principalColumn: "Id");

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
                name: "FK_Calculations_Organisation_OrganisationId",
                table: "Calculations",
                column: "OrganisationId",
                principalTable: "Organisation",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_ProcurementMethod_ProcurementMethodsId",
                table: "Calculations",
                column: "ProcurementMethodsId",
                principalTable: "ProcurementMethod",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_Projects_ProjectId",
                table: "Calculations",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_Users_DeletedBy",
                table: "Calculations",
                column: "DeletedBy",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Calculations_Users_UpdatedBy",
                table: "Calculations",
                column: "UpdatedBy",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CalculationStatus_Users_CreatedBy",
                table: "CalculationStatus",
                column: "CreatedBy",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CalculationStatus_Users_UpdatedBy",
                table: "CalculationStatus",
                column: "UpdatedBy",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Compensations_Users_CreatedBy",
                table: "Compensations",
                column: "CreatedBy",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Compensations_Users_UpdatedBy",
                table: "Compensations",
                column: "UpdatedBy",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Users_CreatedBy",
                table: "Contracts",
                column: "CreatedBy",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Contracts_Users_UpdatedBy",
                table: "Contracts",
                column: "UpdatedBy",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Department_Users_CreatedBy",
                table: "Department",
                column: "CreatedBy",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Department_Users_UpdatedBy",
                table: "Department",
                column: "UpdatedBy",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Department_Users_CreatedBy",
                table: "Department");

            migrationBuilder.DropForeignKey(
                name: "FK_Department_Users_UpdatedBy",
                table: "Department");

            migrationBuilder.DropTable(
                name: "ApplicationValues");

            migrationBuilder.DropTable(
                name: "Offers");

            migrationBuilder.DropTable(
                name: "ShareCalc");

            migrationBuilder.DropTable(
                name: "Storages");

            migrationBuilder.DropTable(
                name: "TenderAttributeBind");

            migrationBuilder.DropTable(
                name: "Applications");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "AttributeNameTender");

            migrationBuilder.DropTable(
                name: "Tenders");

            migrationBuilder.DropTable(
                name: "ResourceSorts");

            migrationBuilder.DropTable(
                name: "ResourceStatus");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "ResourceTypes");

            migrationBuilder.DropTable(
                name: "Opportunity");

            migrationBuilder.DropTable(
                name: "TaskStatus");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Calculations");

            migrationBuilder.DropTable(
                name: "AccountGroup");

            migrationBuilder.DropTable(
                name: "CalculationStatus");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Templates");

            migrationBuilder.DropTable(
                name: "CalcProjectType");

            migrationBuilder.DropTable(
                name: "Compensations");

            migrationBuilder.DropTable(
                name: "Contracts");

            migrationBuilder.DropTable(
                name: "Folders");

            migrationBuilder.DropTable(
                name: "Organisation");

            migrationBuilder.DropTable(
                name: "ProcurementMethod");

            migrationBuilder.DropTable(
                name: "OrganisationCategory");

            migrationBuilder.DropTable(
                name: "OrganisationType");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Department");

            migrationBuilder.DropSequence(
                name: "OrderSeq");
        }
    }
}
