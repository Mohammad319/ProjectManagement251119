using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskResourceBlueprints.Migrations
{
    /// <inheritdoc />
    public partial class DB260617_3 : Migration
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
                    ParentCategoryId = table.Column<int>(type: "int", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceCategories", x => x.Id);
                    table.CheckConstraint("CK_ResourceCategories_Name_NotEmpty", "LEN(LTRIM(RTRIM([DisplayName]))) > 0");
                    table.CheckConstraint("CK_ResourceCategories_NoSelfParent", "[ParentCategoryId] IS NULL OR [ParentCategoryId] <> [Id]");
                    table.CheckConstraint("CK_ResourceCategories_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_ResourceCategories_ResourceCategories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "ResourceCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskResourceSuggestionFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TargetTaskId = table.Column<int>(type: "int", nullable: false),
                    TargetTaskName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TargetTaskCode = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TargetTaskUnit = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TargetTaskQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    SourceTaskId = table.Column<int>(type: "int", nullable: false),
                    SourceTaskName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SourceTaskUnit = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SourceTaskQuantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false),
                    Feedback = table.Column<int>(type: "int", nullable: false),
                    ReviewStatus = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskResourceSuggestionFeedbacks", x => x.Id);
                    table.CheckConstraint("CK_TaskResourceSuggestionFeedbacks_Score_NonNegative", "[Score] >= 0");
                    table.CheckConstraint("CK_TaskResourceSuggestionFeedbacks_SourceQuantity_NonNegative", "[SourceTaskQuantity] IS NULL OR [SourceTaskQuantity] >= 0");
                    table.CheckConstraint("CK_TaskResourceSuggestionFeedbacks_SourceTask_Positive", "[SourceTaskId] > 0");
                    table.CheckConstraint("CK_TaskResourceSuggestionFeedbacks_TargetQuantity_NonNegative", "[TargetTaskQuantity] IS NULL OR [TargetTaskQuantity] >= 0");
                    table.CheckConstraint("CK_TaskResourceSuggestionFeedbacks_TargetTask_Positive", "[TargetTaskId] > 0");
                    table.CheckConstraint("CK_TaskResourceSuggestionFeedbacks_Tenant_Positive", "[TenantId] > 0");
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
                    UnitCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ChangeFactor1 = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ChangeFactor2 = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Uncontrollable = table.Column<bool>(type: "bit", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ParentCode = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ParentName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    HierarchyPath = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    NormalizedTextSv = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    NameSynonyms = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitSynonyms = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsageCount = table.Column<int>(type: "int", nullable: false),
                    WorkloadThresholds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VisibleFolderIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CapacityResourceId = table.Column<int>(type: "int", nullable: true),
                    ConversionParameters = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.CheckConstraint("CK_Tasks_ChangeFactors_Positive", "[ChangeFactor1] > 0 AND [ChangeFactor2] > 0");
                    table.CheckConstraint("CK_Tasks_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_Tasks_PriceProduction_NonNegative", "[PriceProduction] IS NULL OR [PriceProduction] >= 0");
                    table.CheckConstraint("CK_Tasks_Quantity_NonNegative", "[Quantity] IS NULL OR [Quantity] >= 0");
                    table.CheckConstraint("CK_Tasks_SortOrder_NonNegative", "[SortOrder] >= 0");
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
                    table.CheckConstraint("CK_TaskStateGroups_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_TaskStateGroups_SortOrder_NonNegative", "[SortOrder] >= 0");
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
                    Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                    table.CheckConstraint("CK_Resources_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_Resources_Quantity_NonNegative", "[Quantity] IS NULL OR [Quantity] >= 0");
                    table.CheckConstraint("CK_Resources_SortOrder_NonNegative", "[SortOrder] >= 0");
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
                    table.CheckConstraint("CK_TaskStates_Name_NotEmpty", "LEN(LTRIM(RTRIM([Name]))) > 0");
                    table.CheckConstraint("CK_TaskStates_SortOrder_NonNegative", "[SortOrder] >= 0");
                    table.ForeignKey(
                        name: "FK_TaskStates_TaskStateGroups_TaskStateGroupId",
                        column: x => x.TaskStateGroupId,
                        principalTable: "TaskStateGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceTenantLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
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
                    table.CheckConstraint("CK_ResourceTenantLinks_Cost_NonNegative", "[Cost] IS NULL OR [Cost] >= 0");
                    table.CheckConstraint("CK_ResourceTenantLinks_Quantity_NonNegative", "[Quantity] IS NULL OR [Quantity] >= 0");
                    table.CheckConstraint("CK_ResourceTenantLinks_Resource_Positive", "[ResourceId] > 0");
                    table.CheckConstraint("CK_ResourceTenantLinks_Tenant_Positive", "[TenantId] > 0");
                    table.ForeignKey(
                        name: "FK_ResourceTenantLinks_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    IsFixed = table.Column<bool>(type: "bit", nullable: false),
                    Parameters = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddOns = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Times = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskDefinitionResourceLinks", x => x.Id);
                    table.CheckConstraint("CK_TaskDefinitionResourceLinks_Quantity_Positive", "[Quantity] > 0");
                    table.CheckConstraint("CK_TaskDefinitionResourceLinks_Resource_Positive", "[ResourceId] > 0");
                    table.CheckConstraint("CK_TaskDefinitionResourceLinks_Task_Positive", "[TaskDefinitionId] > 0");
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
                name: "UX_ResourceCategories_Parent_Name",
                table: "ResourceCategories",
                columns: new[] { "ParentCategoryId", "DisplayName" },
                unique: true,
                filter: "[ParentCategoryId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_FolderId_SortOrder_Name",
                table: "Resources",
                columns: new[] { "FolderId", "SortOrder", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_IsActive_IsVisible_Name",
                table: "Resources",
                columns: new[] { "IsActive", "IsVisible", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Resources_IsActive_IsVisible_Unit",
                table: "Resources",
                columns: new[] { "IsActive", "IsVisible", "Unit" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTenantLinks_ResourceId",
                table: "ResourceTenantLinks",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "UX_ResourceTenantLink_Tenant_Resource",
                table: "ResourceTenantLinks",
                columns: new[] { "TenantId", "ResourceId" },
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
                name: "IX_Feedback_Tenant_Task_Date",
                table: "TaskResourceSuggestionFeedbacks",
                columns: new[] { "TenantId", "TargetTaskId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskResourceSuggestionFeedbacks_Source_SourceTaskId_Feedback",
                table: "TaskResourceSuggestionFeedbacks",
                columns: new[] { "Source", "SourceTaskId", "Feedback" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskResourceSuggestionFeedbacks_TenantId_ReviewStatus_UpdatedAtUtc",
                table: "TaskResourceSuggestionFeedbacks",
                columns: new[] { "TenantId", "ReviewStatus", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskResourceSuggestionFeedbacks_TenantId_TargetTaskId_Source_SourceTaskId",
                table: "TaskResourceSuggestionFeedbacks",
                columns: new[] { "TenantId", "TargetTaskId", "Source", "SourceTaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Code",
                table: "Tasks",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_HierarchyPath",
                table: "Tasks",
                column: "HierarchyPath");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Name",
                table: "Tasks",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_NormalizedTextSv",
                table: "Tasks",
                column: "NormalizedTextSv");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ParentCode",
                table: "Tasks",
                column: "ParentCode");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Status_SortOrder",
                table: "Tasks",
                columns: new[] { "Status", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Status_UsageCount_Code",
                table: "Tasks",
                columns: new[] { "Status", "UsageCount", "Code" });

            migrationBuilder.CreateIndex(
                name: "UX_TaskStateGroups_Name",
                table: "TaskStateGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskStates_TaskStateGroupId",
                table: "TaskStates",
                column: "TaskStateGroupId");

            migrationBuilder.CreateIndex(
                name: "UX_TaskStates_Group_Name",
                table: "TaskStates",
                columns: new[] { "TaskStateGroupId", "Name" },
                unique: true);
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
                name: "TaskResourceSuggestionFeedbacks");

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
        }
    }
}
