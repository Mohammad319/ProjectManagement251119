using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskResourceBlueprints.Migrations
{
    /// <inheritdoc />
    public partial class DB251107 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ResourceTenant_ResourceId",
                table: "ResourceTenant");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "ResourceTenant");

            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Responsible",
                table: "Tasks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Uncontrollable",
                table: "Tasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Co2",
                table: "ResourceTenant",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Cost",
                table: "ResourceTenant",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "ResourceTenant",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostRole",
                table: "Resources",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultOptionRequirementId",
                table: "QuestionConditions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultResourceRequirementId",
                table: "QuestionConditions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DefaultValue",
                table: "NumericRequirements",
                type: "float",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTenantLink_Tenant_Resource",
                table: "ResourceTenant",
                columns: new[] { "TenantId", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "UX_ResourceTenantLink_Resource_Tenant",
                table: "ResourceTenant",
                columns: new[] { "ResourceId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionConditions_DefaultOptionRequirementId",
                table: "QuestionConditions",
                column: "DefaultOptionRequirementId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionConditions_DefaultResourceRequirementId",
                table: "QuestionConditions",
                column: "DefaultResourceRequirementId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionConditions_ChoiceRequirements_DefaultOptionRequirementId",
                table: "QuestionConditions",
                column: "DefaultOptionRequirementId",
                principalTable: "ChoiceRequirements",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_QuestionConditions_ResourceRequirements_DefaultResourceRequirementId",
                table: "QuestionConditions",
                column: "DefaultResourceRequirementId",
                principalTable: "ResourceRequirements",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuestionConditions_ChoiceRequirements_DefaultOptionRequirementId",
                table: "QuestionConditions");

            migrationBuilder.DropForeignKey(
                name: "FK_QuestionConditions_ResourceRequirements_DefaultResourceRequirementId",
                table: "QuestionConditions");

            migrationBuilder.DropIndex(
                name: "IX_ResourceTenantLink_Tenant_Resource",
                table: "ResourceTenant");

            migrationBuilder.DropIndex(
                name: "UX_ResourceTenantLink_Resource_Tenant",
                table: "ResourceTenant");

            migrationBuilder.DropIndex(
                name: "IX_QuestionConditions_DefaultOptionRequirementId",
                table: "QuestionConditions");

            migrationBuilder.DropIndex(
                name: "IX_QuestionConditions_DefaultResourceRequirementId",
                table: "QuestionConditions");

            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Responsible",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Uncontrollable",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Co2",
                table: "ResourceTenant");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "ResourceTenant");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "ResourceTenant");

            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "CostRole",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "DefaultOptionRequirementId",
                table: "QuestionConditions");

            migrationBuilder.DropColumn(
                name: "DefaultResourceRequirementId",
                table: "QuestionConditions");

            migrationBuilder.DropColumn(
                name: "DefaultValue",
                table: "NumericRequirements");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "ResourceTenant",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTenant_ResourceId",
                table: "ResourceTenant",
                column: "ResourceId");
        }
    }
}
