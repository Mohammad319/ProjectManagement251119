using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DB260326_4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Tenders",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Tenders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Tenders",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tenders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Tenders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TenderAttributeBinds",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "TenderAttributeBinds",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "TenderAttributeBinds",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TenderAttributeBinds",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "TenderAttributeBinds",
                type: "int",
                nullable: true);

            migrationBuilder.DropCheckConstraint(
                name: "CK_Storages_Value_NotEmpty",
                table: "Storages");

            migrationBuilder.AlterColumn<string>(
                name: "StorageValue",
                table: "Storages",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Storages_Value_NotEmpty",
                table: "Storages",
                sql: "LEN(LTRIM(RTRIM([StorageValue]))) > 0");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Opportunities",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Opportunities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Opportunities",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Opportunities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Opportunities",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_CreatedBy",
                table: "Tenders",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tenders_UpdatedBy",
                table: "Tenders",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TenderAttributeBinds_CreatedBy",
                table: "TenderAttributeBinds",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TenderAttributeBinds_UpdatedBy",
                table: "TenderAttributeBinds",
                column: "UpdatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_CreatedBy",
                table: "Opportunities",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_UpdatedBy",
                table: "Opportunities",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Opportunities_Users_CreatedBy",
                table: "Opportunities",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Opportunities_Users_UpdatedBy",
                table: "Opportunities",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TenderAttributeBinds_Users_CreatedBy",
                table: "TenderAttributeBinds",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TenderAttributeBinds_Users_UpdatedBy",
                table: "TenderAttributeBinds",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenders_Users_CreatedBy",
                table: "Tenders",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tenders_Users_UpdatedBy",
                table: "Tenders",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Opportunities_Users_CreatedBy",
                table: "Opportunities");

            migrationBuilder.DropForeignKey(
                name: "FK_Opportunities_Users_UpdatedBy",
                table: "Opportunities");

            migrationBuilder.DropForeignKey(
                name: "FK_TenderAttributeBinds_Users_CreatedBy",
                table: "TenderAttributeBinds");

            migrationBuilder.DropForeignKey(
                name: "FK_TenderAttributeBinds_Users_UpdatedBy",
                table: "TenderAttributeBinds");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenders_Users_CreatedBy",
                table: "Tenders");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenders_Users_UpdatedBy",
                table: "Tenders");

            migrationBuilder.DropIndex(
                name: "IX_Tenders_CreatedBy",
                table: "Tenders");

            migrationBuilder.DropIndex(
                name: "IX_Tenders_UpdatedBy",
                table: "Tenders");

            migrationBuilder.DropIndex(
                name: "IX_TenderAttributeBinds_CreatedBy",
                table: "TenderAttributeBinds");

            migrationBuilder.DropIndex(
                name: "IX_TenderAttributeBinds_UpdatedBy",
                table: "TenderAttributeBinds");

            migrationBuilder.DropIndex(
                name: "IX_Opportunities_CreatedBy",
                table: "Opportunities");

            migrationBuilder.DropIndex(
                name: "IX_Opportunities_UpdatedBy",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Tenders");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TenderAttributeBinds");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "TenderAttributeBinds");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "TenderAttributeBinds");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TenderAttributeBinds");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "TenderAttributeBinds");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Opportunities");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Opportunities");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Storages_Value_NotEmpty",
                table: "Storages");

            migrationBuilder.AlterColumn<string>(
                name: "StorageValue",
                table: "Storages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Storages_Value_NotEmpty",
                table: "Storages",
                sql: "LEN(LTRIM(RTRIM([StorageValue]))) > 0");
        }
    }
}
