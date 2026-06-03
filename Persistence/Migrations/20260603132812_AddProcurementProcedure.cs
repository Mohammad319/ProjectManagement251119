using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProcurementProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProcurementProcedureId",
                table: "Projects",
                type: "int",
                nullable: true);

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
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcurementProcedures", x => x.Id);
                    table.CheckConstraint("CK_ProcurementProcedures_Color_Hex", "[Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'");
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

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProcurementProcedureId",
                table: "Projects",
                column: "ProcurementProcedureId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProcurementProcedures_ProcurementProcedureId",
                table: "Projects",
                column: "ProcurementProcedureId",
                principalTable: "ProcurementProcedures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProcurementProcedures_ProcurementProcedureId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "ProcurementProcedures");

            migrationBuilder.DropIndex(
                name: "IX_Projects_ProcurementProcedureId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ProcurementProcedureId",
                table: "Projects");
        }
    }
}
