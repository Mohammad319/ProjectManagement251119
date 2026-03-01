using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskResourceBlueprints.Migrations
{
    /// <inheritdoc />
    public partial class Blueprints_1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Actions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Falls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Falls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceAttributeSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceAttributeSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourceCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                name: "ResourceAttributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttributeSetId = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsUserEditable = table.Column<bool>(type: "bit", nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    DefaultTextValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultNumericValue = table.Column<double>(type: "float", nullable: true),
                    StepValue = table.Column<double>(type: "float", nullable: true),
                    MaxNumericValue = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceAttributes_ResourceAttributeSets_AttributeSetId",
                        column: x => x.AttributeSetId,
                        principalTable: "ResourceAttributeSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CalcResCost = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    FolderId = table.Column<int>(type: "int", nullable: true),
                    AdminNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CostRoles = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<double>(type: "float", nullable: false),
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
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Responsible = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdminNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FieldNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<double>(type: "float", nullable: true),
                    UnitCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangeFactor1 = table.Column<double>(type: "float", nullable: false),
                    ChangeFactor2 = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Uncontrollable = table.Column<bool>(type: "bit", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkloadThresholds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionId = table.Column<int>(type: "int", nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    ActionTypeId = table.Column<int>(type: "int", nullable: true),
                    FallId = table.Column<int>(type: "int", nullable: true),
                    TaskUnitGroupId = table.Column<int>(type: "int", nullable: true),
                    RowNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VisibleFolderIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CapacityResourceId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasks_ActionTypes_ActionTypeId",
                        column: x => x.ActionTypeId,
                        principalTable: "ActionTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_Actions_ActionId",
                        column: x => x.ActionId,
                        principalTable: "Actions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_Falls_FallId",
                        column: x => x.FallId,
                        principalTable: "Falls",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_Locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "Locations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tasks_TaskUnitGroups_TaskUnitGroupId",
                        column: x => x.TaskUnitGroupId,
                        principalTable: "TaskUnitGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResourceAttributeValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    AttributeId = table.Column<int>(type: "int", nullable: false),
                    TextValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumericValue = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceAttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceAttributeValues_ResourceAttributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "ResourceAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceAttributeValues_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
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
                    Cost = table.Column<double>(type: "float", nullable: true),
                    Quantity = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceTenantLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceTenantLinks_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NumericQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    MinInputValue = table.Column<double>(type: "float", nullable: true),
                    MaxInputValue = table.Column<double>(type: "float", nullable: true),
                    SectionKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TaskId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumericQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NumericQuestions_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SelectionMode = table.Column<int>(type: "int", nullable: false),
                    SectionKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TaskId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionGroups_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceSelectors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SectionKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TaskId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceSelectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceSelectors_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskResourceAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ChangeFactor1 = table.Column<double>(type: "float", nullable: false),
                    ChangeFactor2 = table.Column<double>(type: "float", nullable: false),
                    CapWaste = table.Column<double>(type: "float", nullable: false),
                    BaseCost = table.Column<double>(type: "float", nullable: true),
                    Uncontrollable = table.Column<bool>(type: "bit", nullable: false),
                    CapacityRoles = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Expressions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MenuId = table.Column<int>(type: "int", nullable: true),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskResourceAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskResourceAssignments_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskResourceAssignments_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionGroupId = table.Column<int>(type: "int", nullable: false),
                    RevealedSectionKeys = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionOptions_QuestionGroups_QuestionGroupId",
                        column: x => x.QuestionGroupId,
                        principalTable: "QuestionGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceChoiceOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SelectorId = table.Column<int>(type: "int", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceChoiceOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceChoiceOptions_ResourceSelectors_SelectorId",
                        column: x => x.SelectorId,
                        principalTable: "ResourceSelectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceChoiceOptions_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConditionResourceAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    ConditionId = table.Column<int>(type: "int", nullable: false),
                    MenuId = table.Column<int>(type: "int", nullable: true),
                    ChangeFactor1 = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    ChangeFactor2 = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    CapWaste = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    BaseCost = table.Column<double>(type: "float(18)", precision: 18, scale: 4, nullable: true),
                    Uncontrollable = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CapacityRoles = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Expressions = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConditionResourceAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConditionResourceAssignments_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NumericResourceAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumericId = table.Column<int>(type: "int", nullable: false),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    MinInputValue = table.Column<double>(type: "float", nullable: true),
                    MaxInputValue = table.Column<double>(type: "float", nullable: true),
                    Expressions = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumericResourceAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NumericResourceAssignments_ConditionResourceAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "ConditionResourceAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NumericResourceAssignments_NumericQuestions_NumericId",
                        column: x => x.NumericId,
                        principalTable: "NumericQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OptionResourceAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OptionId = table.Column<int>(type: "int", nullable: false),
                    AssignmentId = table.Column<int>(type: "int", nullable: false),
                    Expressions = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionResourceAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptionResourceAssignments_ConditionResourceAssignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "ConditionResourceAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OptionResourceAssignments_QuestionOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "QuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Conditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OptionResourceLogic = table.Column<int>(type: "int", nullable: false),
                    OptionNumericLogic = table.Column<int>(type: "int", nullable: false),
                    NumericResourceLogic = table.Column<int>(type: "int", nullable: false),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    DefaultOptionRuleId = table.Column<int>(type: "int", nullable: true),
                    DefaultResourceRuleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conditions_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NumericRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConditionId = table.Column<int>(type: "int", nullable: false),
                    NumericQuestionId = table.Column<int>(type: "int", nullable: false),
                    MaxAllowedValue = table.Column<double>(type: "float", nullable: true),
                    MinAllowedValue = table.Column<double>(type: "float", nullable: true),
                    DefaultValue = table.Column<double>(type: "float", nullable: true),
                    GroupKey = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumericRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NumericRequirements_Conditions_ConditionId",
                        column: x => x.ConditionId,
                        principalTable: "Conditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NumericRequirements_NumericQuestions_NumericQuestionId",
                        column: x => x.NumericQuestionId,
                        principalTable: "NumericQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OptionRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConditionId = table.Column<int>(type: "int", nullable: false),
                    QuestionGroupId = table.Column<int>(type: "int", nullable: false),
                    OptionId = table.Column<int>(type: "int", nullable: false),
                    GroupKey = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptionRequirements_Conditions_ConditionId",
                        column: x => x.ConditionId,
                        principalTable: "Conditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OptionRequirements_QuestionGroups_QuestionGroupId",
                        column: x => x.QuestionGroupId,
                        principalTable: "QuestionGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OptionRequirements_QuestionOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "QuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResourceRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConditionId = table.Column<int>(type: "int", nullable: false),
                    SelectorId = table.Column<int>(type: "int", nullable: false),
                    SelectorItemId = table.Column<int>(type: "int", nullable: false),
                    GroupKey = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceRequirements_Conditions_ConditionId",
                        column: x => x.ConditionId,
                        principalTable: "Conditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceRequirements_ResourceChoiceOptions_SelectorItemId",
                        column: x => x.SelectorItemId,
                        principalTable: "ResourceChoiceOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceRequirements_ResourceSelectors_SelectorId",
                        column: x => x.SelectorId,
                        principalTable: "ResourceSelectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VariableConditionRule",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConditionId = table.Column<int>(type: "int", nullable: false),
                    VariableName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinAllowedValue = table.Column<double>(type: "float", nullable: true),
                    MaxAllowedValue = table.Column<double>(type: "float", nullable: true),
                    GroupKey = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariableConditionRule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VariableConditionRule_Conditions_ConditionId",
                        column: x => x.ConditionId,
                        principalTable: "Conditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConditionResourceAssignments_ConditionId",
                table: "ConditionResourceAssignments",
                column: "ConditionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConditionResourceAssignments_ResourceId",
                table: "ConditionResourceAssignments",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_Conditions_DefaultOptionRuleId",
                table: "Conditions",
                column: "DefaultOptionRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Conditions_DefaultResourceRuleId",
                table: "Conditions",
                column: "DefaultResourceRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Conditions_TaskId",
                table: "Conditions",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_NumericQuestions_TaskId_SectionKey_SortOrder",
                table: "NumericQuestions",
                columns: new[] { "TaskId", "SectionKey", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_NumericQuestions_TaskId_SortOrder",
                table: "NumericQuestions",
                columns: new[] { "TaskId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_NumericRequirements_ConditionId_GroupKey",
                table: "NumericRequirements",
                columns: new[] { "ConditionId", "GroupKey" });

            migrationBuilder.CreateIndex(
                name: "IX_NumericRequirements_NumericQuestionId",
                table: "NumericRequirements",
                column: "NumericQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_NumericResourceAssignments_AssignmentId",
                table: "NumericResourceAssignments",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_NumericResourceAssignments_NumericId_AssignmentId",
                table: "NumericResourceAssignments",
                columns: new[] { "NumericId", "AssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OptionRequirements_ConditionId_QuestionGroupId_OptionId_GroupKey",
                table: "OptionRequirements",
                columns: new[] { "ConditionId", "QuestionGroupId", "OptionId", "GroupKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OptionRequirements_OptionId",
                table: "OptionRequirements",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_OptionRequirements_QuestionGroupId",
                table: "OptionRequirements",
                column: "QuestionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_OptionResourceAssignments_AssignmentId",
                table: "OptionResourceAssignments",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OptionResourceAssignments_OptionId_AssignmentId",
                table: "OptionResourceAssignments",
                columns: new[] { "OptionId", "AssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestionGroups_TaskId_SectionKey_SortOrder",
                table: "QuestionGroups",
                columns: new[] { "TaskId", "SectionKey", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionGroups_TaskId_SortOrder",
                table: "QuestionGroups",
                columns: new[] { "TaskId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_QuestionGroupId",
                table: "QuestionOptions",
                column: "QuestionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAttributes_AttributeSetId",
                table: "ResourceAttributes",
                column: "AttributeSetId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAttributeValues_AttributeId",
                table: "ResourceAttributeValues",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAttributeValues_ResourceId",
                table: "ResourceAttributeValues",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceCategories_ParentCategoryId",
                table: "ResourceCategories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceChoiceOptions_ResourceId",
                table: "ResourceChoiceOptions",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceChoiceOptions_SelectorId_ResourceId",
                table: "ResourceChoiceOptions",
                columns: new[] { "SelectorId", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequirements_ConditionId_SelectorId_SelectorItemId_GroupKey",
                table: "ResourceRequirements",
                columns: new[] { "ConditionId", "SelectorId", "SelectorItemId", "GroupKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequirements_SelectorId",
                table: "ResourceRequirements",
                column: "SelectorId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequirements_SelectorItemId",
                table: "ResourceRequirements",
                column: "SelectorItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_FolderId",
                table: "Resources",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceSelectors_TaskId_SectionKey_SortOrder",
                table: "ResourceSelectors",
                columns: new[] { "TaskId", "SectionKey", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceSelectors_TaskId_SortOrder",
                table: "ResourceSelectors",
                columns: new[] { "TaskId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTenantLink_Tenant_Resource",
                table: "ResourceTenantLinks",
                columns: new[] { "TenantId", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "UX_ResourceTenantLink_Resource_Tenant",
                table: "ResourceTenantLinks",
                columns: new[] { "ResourceId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskResourceAssignments_ResourceId",
                table: "TaskResourceAssignments",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskResourceAssignments_TaskId",
                table: "TaskResourceAssignments",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ActionId",
                table: "Tasks",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ActionTypeId",
                table: "Tasks",
                column: "ActionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_FallId",
                table: "Tasks",
                column: "FallId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_LocationId",
                table: "Tasks",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_TaskUnitGroupId",
                table: "Tasks",
                column: "TaskUnitGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_VariableConditionRule_ConditionId",
                table: "VariableConditionRule",
                column: "ConditionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConditionResourceAssignments_Conditions_ConditionId",
                table: "ConditionResourceAssignments",
                column: "ConditionId",
                principalTable: "Conditions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Conditions_OptionRequirements_DefaultOptionRuleId",
                table: "Conditions",
                column: "DefaultOptionRuleId",
                principalTable: "OptionRequirements",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Conditions_ResourceRequirements_DefaultResourceRuleId",
                table: "Conditions",
                column: "DefaultResourceRuleId",
                principalTable: "ResourceRequirements",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OptionRequirements_Conditions_ConditionId",
                table: "OptionRequirements");

            migrationBuilder.DropForeignKey(
                name: "FK_ResourceRequirements_Conditions_ConditionId",
                table: "ResourceRequirements");

            migrationBuilder.DropTable(
                name: "NumericRequirements");

            migrationBuilder.DropTable(
                name: "NumericResourceAssignments");

            migrationBuilder.DropTable(
                name: "OptionResourceAssignments");

            migrationBuilder.DropTable(
                name: "ResourceAttributeValues");

            migrationBuilder.DropTable(
                name: "ResourceTenantLinks");

            migrationBuilder.DropTable(
                name: "TaskResourceAssignments");

            migrationBuilder.DropTable(
                name: "VariableConditionRule");

            migrationBuilder.DropTable(
                name: "NumericQuestions");

            migrationBuilder.DropTable(
                name: "ConditionResourceAssignments");

            migrationBuilder.DropTable(
                name: "ResourceAttributes");

            migrationBuilder.DropTable(
                name: "ResourceAttributeSets");

            migrationBuilder.DropTable(
                name: "Conditions");

            migrationBuilder.DropTable(
                name: "OptionRequirements");

            migrationBuilder.DropTable(
                name: "ResourceRequirements");

            migrationBuilder.DropTable(
                name: "QuestionOptions");

            migrationBuilder.DropTable(
                name: "ResourceChoiceOptions");

            migrationBuilder.DropTable(
                name: "QuestionGroups");

            migrationBuilder.DropTable(
                name: "ResourceSelectors");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "ResourceCategories");

            migrationBuilder.DropTable(
                name: "ActionTypes");

            migrationBuilder.DropTable(
                name: "Actions");

            migrationBuilder.DropTable(
                name: "Falls");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "TaskUnitGroups");
        }
    }
}
