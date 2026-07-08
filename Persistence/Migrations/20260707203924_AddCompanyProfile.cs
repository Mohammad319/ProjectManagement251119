using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OrgNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    VatNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LogoData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    LogoContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PreviousLogoData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    PreviousLogoContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReportHeaderText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReportFooterText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CalcReportDefaultText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TenderReportDefaultText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SelfInspectionReportDefaultText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DisclaimerText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NameLastChangedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LogoChangeHistory = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyProfiles_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyProfiles_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_CreatedBy",
                table: "CompanyProfiles",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_Tenant",
                table: "CompanyProfiles",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_UpdatedBy",
                table: "CompanyProfiles",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyProfiles");
        }
    }
}
