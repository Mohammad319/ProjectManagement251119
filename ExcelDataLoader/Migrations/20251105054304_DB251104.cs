using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectImportHub.Migrations
{
    /// <inheritdoc />
    public partial class DB251104 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResourceCondetionTenant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    ResId = table.Column<int>(type: "int", nullable: true),
                    SortId = table.Column<int>(type: "int", nullable: true),
                    AccountId = table.Column<int>(type: "int", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "ResourceTenant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    ResId = table.Column<int>(type: "int", nullable: true),
                    SortId = table.Column<int>(type: "int", nullable: true),
                    AccountId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceTenant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceTenant_TaskResourceAssignments_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "TaskResourceAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceCondetionTenant_ResourceId",
                table: "ResourceCondetionTenant",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTenant_ResourceId",
                table: "ResourceTenant",
                column: "ResourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceCondetionTenant");

            migrationBuilder.DropTable(
                name: "ResourceTenant");
        }
    }
}
