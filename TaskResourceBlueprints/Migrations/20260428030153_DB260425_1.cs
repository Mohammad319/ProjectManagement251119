using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskResourceBlueprints.Migrations
{
    /// <inheritdoc />
    public partial class DB260425_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResourceCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceCategories_ResourceCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "ResourceCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskStateGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskStateGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskUnitGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Keys = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskUnitGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    FolderId = table.Column<int>(type: "int", nullable: true),
                    AdminNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CostRoles = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resources_ResourceCategories_FolderId",
                        column: x => x.FolderId,
                        principalTable: "ResourceCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskStateGroupId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskStates_TaskStateGroups_TaskStateGroupId",
                        column: x => x.TaskStateGroupId,
                        principalTable: "TaskStateGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Responsible = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdminNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FieldNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    PriceProduction = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    UnitCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangeFactor1 = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ChangeFactor2 = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Uncontrollable = table.Column<bool>(type: "bit", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedTextSv = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    WorkloadThresholds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaskUnitGroupId = table.Column<int>(type: "int", nullable: true),
                    RowNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VisibleFolderIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CapacityResourceId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasks_TaskUnitGroups_TaskUnitGroupId",
                        column: x => x.TaskUnitGroupId,
                        principalTable: "TaskUnitGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResourceTenantLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    ResourceDefinitionId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusId = table.Column<int>(type: "int", nullable: true),
                    ResourceTypeId = table.Column<int>(type: "int", nullable: true),
                    ResourceSortId = table.Column<int>(type: "int", nullable: true),
                    AccountId = table.Column<int>(type: "int", nullable: true),
                    Co2 = table.Column<double>(type: "float", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceTenantLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceTenantLinks_Resources_ResourceDefinitionId",
                        column: x => x.ResourceDefinitionId,
                        principalTable: "Resources",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskDefinitionResourceLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskDefinitionId = table.Column<int>(type: "int", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsFixed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDefinitionResourceLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskDefinitionResourceLinks_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskDefinitionResourceLinks_Tasks_TaskDefinitionId",
                        column: x => x.TaskDefinitionId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskDefinitionStateLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskDefinitionId = table.Column<int>(type: "int", nullable: false),
                    TaskStateId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDefinitionStateLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskDefinitionStateLinks_TaskStates_TaskStateId",
                        column: x => x.TaskStateId,
                        principalTable: "TaskStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskDefinitionStateLinks_Tasks_TaskDefinitionId",
                        column: x => x.TaskDefinitionId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceCategories_ParentCategoryId_SortOrder_DisplayName",
                table: "ResourceCategories",
                columns: new[] { "ParentCategoryId", "SortOrder", "DisplayName" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_FolderId_SortOrder_Name",
                table: "Resources",
                columns: new[] { "FolderId", "SortOrder", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_IsActive_IsVisible_Name",
                table: "Resources",
                columns: new[] { "IsActive", "IsVisible", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTenantLink_Tenant_Resource",
                table: "ResourceTenantLinks",
                columns: new[] { "TenantId", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTenantLinks_ResourceDefinitionId",
                table: "ResourceTenantLinks",
                column: "ResourceDefinitionId");

            migrationBuilder.CreateIndex(
                name: "UX_ResourceTenantLink_Resource_Tenant",
                table: "ResourceTenantLinks",
                columns: new[] { "ResourceId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskDefinitionResourceLinks_ResourceId",
                table: "TaskDefinitionResourceLinks",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskDefinitionResourceLinks_TaskDefinitionId_ResourceId",
                table: "TaskDefinitionResourceLinks",
                columns: new[] { "TaskDefinitionId", "ResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskDefinitionStateLinks_TaskDefinitionId_TaskStateId",
                table: "TaskDefinitionStateLinks",
                columns: new[] { "TaskDefinitionId", "TaskStateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskDefinitionStateLinks_TaskStateId",
                table: "TaskDefinitionStateLinks",
                column: "TaskStateId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_NormalizedTextSv",
                table: "Tasks",
                column: "NormalizedTextSv");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Status_SortOrder",
                table: "Tasks",
                columns: new[] { "Status", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_TaskUnitGroupId",
                table: "Tasks",
                column: "TaskUnitGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskStates_TaskStateGroupId",
                table: "TaskStates",
                column: "TaskStateGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceTenantLinks");

            migrationBuilder.DropTable(
                name: "TaskDefinitionResourceLinks");

            migrationBuilder.DropTable(
                name: "TaskDefinitionStateLinks");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "TaskStates");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "ResourceCategories");

            migrationBuilder.DropTable(
                name: "TaskStateGroups");

            migrationBuilder.DropTable(
                name: "TaskUnitGroups");
        }
    }
}
