using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountImportBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountImportBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ImportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GroupsCreated = table.Column<int>(type: "int", nullable: false),
                    AccountsCreated = table.Column<int>(type: "int", nullable: false),
                    AccountsUpdated = table.Column<int>(type: "int", nullable: false),
                    AccountsSkipped = table.Column<int>(type: "int", nullable: false),
                    CreatedGroupIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAccountIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsUndone = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountImportBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountImportBatches_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountImportBatches_Users_UpdatedBy",
                        column: x => x.UpdatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountImportBatches_CreatedBy",
                table: "AccountImportBatches",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AccountImportBatches_UpdatedBy",
                table: "AccountImportBatches",
                column: "UpdatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountImportBatches");
        }
    }
}
