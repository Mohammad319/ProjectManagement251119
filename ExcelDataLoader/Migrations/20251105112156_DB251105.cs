using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectImportHub.Migrations
{
    /// <inheritdoc />
    public partial class DB251105 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResourceTenant_TaskResourceAssignments_ResourceId",
                table: "ResourceTenant");

            migrationBuilder.DropTable(
                name: "ResourceCondetionTenant");

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceTenant_Resources_ResourceId",
                table: "ResourceTenant",
                column: "ResourceId",
                principalTable: "Resources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ResourceTenant_Resources_ResourceId",
                table: "ResourceTenant");

            migrationBuilder.CreateTable(
                name: "ResourceCondetionTenant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResId = table.Column<int>(type: "int", nullable: true),
                    SortId = table.Column<int>(type: "int", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceCondetionTenant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceCondetionTenant_ConditionResourceAssignments_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "ConditionResourceAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceCondetionTenant_ResourceId",
                table: "ResourceCondetionTenant",
                column: "ResourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_ResourceTenant_TaskResourceAssignments_ResourceId",
                table: "ResourceTenant",
                column: "ResourceId",
                principalTable: "TaskResourceAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
