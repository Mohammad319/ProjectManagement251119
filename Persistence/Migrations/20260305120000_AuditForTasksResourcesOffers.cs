using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Persistence.Context;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ShardingSingleDbContext))]
    [Migration("20260305120000_AuditForTasksResourcesOffers")]
    public partial class AuditForTasksResourcesOffers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // -----------------------------
            // Tasks
            // -----------------------------
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("UPDATE dbo.Tasks SET CreatedAt = ISNULL(CreatedAt, SYSUTCDATETIME());");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Tasks",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CreatedBy",
                table: "Tasks",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_UpdatedBy",
                table: "Tasks",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Users_CreatedBy",
                table: "Tasks",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Users_UpdatedBy",
                table: "Tasks",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // -----------------------------
            // Resources
            // -----------------------------
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Resources",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("UPDATE dbo.Resources SET CreatedAt = ISNULL(CreatedAt, SYSUTCDATETIME());");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Resources",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Resources",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Resources",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Resources",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resources_CreatedBy",
                table: "Resources",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_UpdatedBy",
                table: "Resources",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Users_CreatedBy",
                table: "Resources",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Resources_Users_UpdatedBy",
                table: "Resources",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // -----------------------------
            // Offers
            // -----------------------------
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Offers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("UPDATE dbo.Offers SET CreatedAt = ISNULL(CreatedAt, SYSUTCDATETIME());");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Offers",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Offers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Offers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "Offers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_CreatedBy",
                table: "Offers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_UpdatedBy",
                table: "Offers",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Users_CreatedBy",
                table: "Offers",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Offers_Users_UpdatedBy",
                table: "Offers",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // -----------------------------
            // Data integrity constraints (trimmed name not empty)
            // -----------------------------
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Tasks_Name_NotEmpty' AND parent_object_id = OBJECT_ID('dbo.Tasks'))
    ALTER TABLE [dbo].[Tasks] WITH NOCHECK ADD CONSTRAINT [CK_Tasks_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0);

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Resources_Name_NotEmpty' AND parent_object_id = OBJECT_ID('dbo.Resources'))
    ALTER TABLE [dbo].[Resources] WITH NOCHECK ADD CONSTRAINT [CK_Resources_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Tasks_Name_NotEmpty' AND parent_object_id = OBJECT_ID('dbo.Tasks'))
    ALTER TABLE [dbo].[Tasks] DROP CONSTRAINT [CK_Tasks_Name_NotEmpty];

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Resources_Name_NotEmpty' AND parent_object_id = OBJECT_ID('dbo.Resources'))
    ALTER TABLE [dbo].[Resources] DROP CONSTRAINT [CK_Resources_Name_NotEmpty];
");

            // Offers
            migrationBuilder.DropForeignKey(name: "FK_Offers_Users_CreatedBy", table: "Offers");
            migrationBuilder.DropForeignKey(name: "FK_Offers_Users_UpdatedBy", table: "Offers");
            migrationBuilder.DropIndex(name: "IX_Offers_CreatedBy", table: "Offers");
            migrationBuilder.DropIndex(name: "IX_Offers_UpdatedBy", table: "Offers");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "Offers");
            migrationBuilder.DropColumn(name: "CreatedBy", table: "Offers");
            migrationBuilder.DropColumn(name: "UpdatedAt", table: "Offers");
            migrationBuilder.DropColumn(name: "UpdatedBy", table: "Offers");

            // Resources
            migrationBuilder.DropForeignKey(name: "FK_Resources_Users_CreatedBy", table: "Resources");
            migrationBuilder.DropForeignKey(name: "FK_Resources_Users_UpdatedBy", table: "Resources");
            migrationBuilder.DropIndex(name: "IX_Resources_CreatedBy", table: "Resources");
            migrationBuilder.DropIndex(name: "IX_Resources_UpdatedBy", table: "Resources");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "Resources");
            migrationBuilder.DropColumn(name: "CreatedBy", table: "Resources");
            migrationBuilder.DropColumn(name: "UpdatedAt", table: "Resources");
            migrationBuilder.DropColumn(name: "UpdatedBy", table: "Resources");

            // Tasks
            migrationBuilder.DropForeignKey(name: "FK_Tasks_Users_CreatedBy", table: "Tasks");
            migrationBuilder.DropForeignKey(name: "FK_Tasks_Users_UpdatedBy", table: "Tasks");
            migrationBuilder.DropIndex(name: "IX_Tasks_CreatedBy", table: "Tasks");
            migrationBuilder.DropIndex(name: "IX_Tasks_UpdatedBy", table: "Tasks");
            migrationBuilder.DropColumn(name: "CreatedAt", table: "Tasks");
            migrationBuilder.DropColumn(name: "CreatedBy", table: "Tasks");
            migrationBuilder.DropColumn(name: "UpdatedAt", table: "Tasks");
            migrationBuilder.DropColumn(name: "UpdatedBy", table: "Tasks");
        }
    }
}
