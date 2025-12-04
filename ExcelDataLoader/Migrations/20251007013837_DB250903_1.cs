using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectImportHub.Migrations
{
    /// <inheritdoc />
    public partial class DB250903_1 : Migration
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
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Falls", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Folders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ParentFolderId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Folders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Folders_Folders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "Folders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResourcePropertySets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourcePropertySets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnitGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Keys = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FolderId = table.Column<int>(type: "int", nullable: true),
                    Formulas = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResType = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<double>(type: "float", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GroupId = table.Column<int>(type: "int", nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resources_Folders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "Folders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResourceProperties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertySetId = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsUserEditable = table.Column<bool>(type: "bit", nullable: false),
                    DataType = table.Column<int>(type: "int", nullable: false),
                    DefaultTextValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultNumericValue = table.Column<double>(type: "float", nullable: true),
                    PropertyValues = table.Column<double>(type: "float", nullable: true),
                    MaxNumericValue = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceProperties_ResourcePropertySets_PropertySetId",
                        column: x => x.PropertySetId,
                        principalTable: "ResourcePropertySets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HeaderNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<double>(type: "float", nullable: true),
                    UnitCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangeFactor1 = table.Column<double>(type: "float", nullable: false),
                    ChangeFactor2 = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Formulas = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkloadThresholds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionId = table.Column<int>(type: "int", nullable: true),
                    LocationId = table.Column<int>(type: "int", nullable: true),
                    ActionTypeId = table.Column<int>(type: "int", nullable: true),
                    FallId = table.Column<int>(type: "int", nullable: true),
                    UnitGroupId = table.Column<int>(type: "int", nullable: true),
                    VisibleFolderIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CapacityResourceId = table.Column<int>(type: "int", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                        name: "FK_Tasks_UnitGroups_UnitGroupId",
                        column: x => x.UnitGroupId,
                        principalTable: "UnitGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ResourceProperty",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    TextDefault = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberDefault = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceProperty", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceProperty_ResourceProperties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "ResourceProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceProperty_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NumericInputGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    MinInputValue = table.Column<double>(type: "float", nullable: true),
                    MaxInputValue = table.Column<double>(type: "float", nullable: true),
                    SectionKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumericInputGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NumericInputGroups_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OptionGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SelectionMode = table.Column<int>(type: "int", nullable: false),
                    SectionKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptionGroups_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuestionConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    OptionToResourceLogic = table.Column<int>(type: "int", nullable: false),
                    OptionToNumericLogic = table.Column<int>(type: "int", nullable: false),
                    NumericToResourceLogic = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestionConditions_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceOptionGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    SectionKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceOptionGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceOptionGroups_Tasks_TaskId",
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
                    TaskId = table.Column<int>(type: "int", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ChangeFactor1 = table.Column<double>(type: "float", nullable: false),
                    ChangeFactor2 = table.Column<double>(type: "float", nullable: false),
                    CapWaste = table.Column<double>(type: "float", nullable: false),
                    BaseCost = table.Column<double>(type: "float", nullable: true),
                    Uncontrollable = table.Column<bool>(type: "bit", nullable: false),
                    CapRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Formulas = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                name: "OptionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionGroupId = table.Column<int>(type: "int", nullable: false),
                    RevealedSectionKeys = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptionItems_OptionGroups_OptionGroupId",
                        column: x => x.OptionGroupId,
                        principalTable: "OptionGroups",
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
                    QuestionConditionId = table.Column<int>(type: "int", nullable: false),
                    ChangeFactor1 = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    ChangeFactor2 = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    CapWaste = table.Column<double>(type: "float(18)", precision: 18, scale: 6, nullable: false),
                    BaseCost = table.Column<double>(type: "float(18)", precision: 18, scale: 4, nullable: true),
                    Uncontrollable = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CapRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Formulas = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConditionResourceAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConditionResourceAssignments_QuestionConditions_QuestionConditionId",
                        column: x => x.QuestionConditionId,
                        principalTable: "QuestionConditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConditionResourceAssignments_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConditionVariableRequirement",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionConditionId = table.Column<int>(type: "int", nullable: false),
                    VariableName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinAllowedValue = table.Column<double>(type: "float", nullable: true),
                    MaxAllowedValue = table.Column<double>(type: "float", nullable: true),
                    SetKey = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConditionVariableRequirement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConditionVariableRequirement_QuestionConditions_QuestionConditionId",
                        column: x => x.QuestionConditionId,
                        principalTable: "QuestionConditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NumericRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionConditionId = table.Column<int>(type: "int", nullable: false),
                    NumericInputId = table.Column<int>(type: "int", nullable: false),
                    MaxAllowedValue = table.Column<double>(type: "float", nullable: true),
                    MinAllowedValue = table.Column<double>(type: "float", nullable: true),
                    SetKey = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumericRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NumericRequirements_NumericInputGroups_NumericInputId",
                        column: x => x.NumericInputId,
                        principalTable: "NumericInputGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NumericRequirements_QuestionConditions_QuestionConditionId",
                        column: x => x.QuestionConditionId,
                        principalTable: "QuestionConditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceOptionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceChoiceGroupId = table.Column<int>(type: "int", nullable: false),
                    ResourceId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceOptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceOptionItems_ResourceOptionGroups_ResourceChoiceGroupId",
                        column: x => x.ResourceChoiceGroupId,
                        principalTable: "ResourceOptionGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceOptionItems_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChoiceRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionConditionId = table.Column<int>(type: "int", nullable: false),
                    OptionGroupId = table.Column<int>(type: "int", nullable: false),
                    OptionItemId = table.Column<int>(type: "int", nullable: false),
                    SetKey = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChoiceRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChoiceRequirements_OptionGroups_OptionGroupId",
                        column: x => x.OptionGroupId,
                        principalTable: "OptionGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChoiceRequirements_OptionItems_OptionItemId",
                        column: x => x.OptionItemId,
                        principalTable: "OptionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChoiceRequirements_QuestionConditions_QuestionConditionId",
                        column: x => x.QuestionConditionId,
                        principalTable: "QuestionConditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NumericGroupResourceAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumericId = table.Column<int>(type: "int", nullable: false),
                    ResourceAssignmentId = table.Column<int>(type: "int", nullable: false),
                    MinInputValue = table.Column<double>(type: "float", nullable: true),
                    MaxInputValue = table.Column<double>(type: "float", nullable: true),
                    Formulas = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumericGroupResourceAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NumericGroupResourceAssignments_ConditionResourceAssignments_ResourceAssignmentId",
                        column: x => x.ResourceAssignmentId,
                        principalTable: "ConditionResourceAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NumericGroupResourceAssignments_NumericInputGroups_NumericId",
                        column: x => x.NumericId,
                        principalTable: "NumericInputGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OptionResourceAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ChoiceOptionId = table.Column<int>(type: "int", nullable: false),
                    ResourceAssignmentId = table.Column<int>(type: "int", nullable: false),
                    Formulas = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionResourceAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptionResourceAssignments_ConditionResourceAssignments_ResourceAssignmentId",
                        column: x => x.ResourceAssignmentId,
                        principalTable: "ConditionResourceAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OptionResourceAssignments_OptionItems_ChoiceOptionId",
                        column: x => x.ChoiceOptionId,
                        principalTable: "OptionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionConditionId = table.Column<int>(type: "int", nullable: false),
                    ResourceOptionGroupId = table.Column<int>(type: "int", nullable: false),
                    ResourceOptionItemId = table.Column<int>(type: "int", nullable: false),
                    SetKey = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceRequirements_QuestionConditions_QuestionConditionId",
                        column: x => x.QuestionConditionId,
                        principalTable: "QuestionConditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceRequirements_ResourceOptionGroups_ResourceOptionGroupId",
                        column: x => x.ResourceOptionGroupId,
                        principalTable: "ResourceOptionGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceRequirements_ResourceOptionItems_ResourceOptionItemId",
                        column: x => x.ResourceOptionItemId,
                        principalTable: "ResourceOptionItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChoiceRequirements_OptionGroupId",
                table: "ChoiceRequirements",
                column: "OptionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ChoiceRequirements_OptionItemId",
                table: "ChoiceRequirements",
                column: "OptionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ChoiceRequirements_QuestionConditionId_OptionGroupId_OptionItemId_SetKey",
                table: "ChoiceRequirements",
                columns: new[] { "QuestionConditionId", "OptionGroupId", "OptionItemId", "SetKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConditionResourceAssignments_QuestionConditionId",
                table: "ConditionResourceAssignments",
                column: "QuestionConditionId");

            migrationBuilder.CreateIndex(
                name: "IX_ConditionResourceAssignments_ResourceId",
                table: "ConditionResourceAssignments",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ConditionVariableRequirement_QuestionConditionId",
                table: "ConditionVariableRequirement",
                column: "QuestionConditionId");

            migrationBuilder.CreateIndex(
                name: "IX_Folders_ParentFolderId",
                table: "Folders",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_NumericGroupResourceAssignments_NumericId_ResourceAssignmentId",
                table: "NumericGroupResourceAssignments",
                columns: new[] { "NumericId", "ResourceAssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NumericGroupResourceAssignments_ResourceAssignmentId",
                table: "NumericGroupResourceAssignments",
                column: "ResourceAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_NumericInputGroups_TaskId_SectionKey_SortOrder",
                table: "NumericInputGroups",
                columns: new[] { "TaskId", "SectionKey", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_NumericInputGroups_TaskId_SortOrder",
                table: "NumericInputGroups",
                columns: new[] { "TaskId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_NumericRequirements_NumericInputId",
                table: "NumericRequirements",
                column: "NumericInputId");

            migrationBuilder.CreateIndex(
                name: "IX_NumericRequirements_QuestionConditionId_SetKey",
                table: "NumericRequirements",
                columns: new[] { "QuestionConditionId", "SetKey" });

            migrationBuilder.CreateIndex(
                name: "IX_OptionGroups_TaskId_SectionKey_SortOrder",
                table: "OptionGroups",
                columns: new[] { "TaskId", "SectionKey", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_OptionGroups_TaskId_SortOrder",
                table: "OptionGroups",
                columns: new[] { "TaskId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_OptionItems_OptionGroupId",
                table: "OptionItems",
                column: "OptionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_OptionResourceAssignments_ChoiceOptionId_ResourceAssignmentId",
                table: "OptionResourceAssignments",
                columns: new[] { "ChoiceOptionId", "ResourceAssignmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OptionResourceAssignments_ResourceAssignmentId",
                table: "OptionResourceAssignments",
                column: "ResourceAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionConditions_TaskId",
                table: "QuestionConditions",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceOptionGroups_TaskId_SectionKey_SortOrder",
                table: "ResourceOptionGroups",
                columns: new[] { "TaskId", "SectionKey", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceOptionGroups_TaskId_SortOrder",
                table: "ResourceOptionGroups",
                columns: new[] { "TaskId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceOptionItems_ResourceChoiceGroupId_ResourceId",
                table: "ResourceOptionItems",
                columns: new[] { "ResourceChoiceGroupId", "ResourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceOptionItems_ResourceId",
                table: "ResourceOptionItems",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceProperties_PropertySetId",
                table: "ResourceProperties",
                column: "PropertySetId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceProperty_PropertyId",
                table: "ResourceProperty",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceProperty_ResourceId",
                table: "ResourceProperty",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequirements_QuestionConditionId_ResourceOptionGroupId_ResourceOptionItemId_SetKey",
                table: "ResourceRequirements",
                columns: new[] { "QuestionConditionId", "ResourceOptionGroupId", "ResourceOptionItemId", "SetKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequirements_ResourceOptionGroupId",
                table: "ResourceRequirements",
                column: "ResourceOptionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequirements_ResourceOptionItemId",
                table: "ResourceRequirements",
                column: "ResourceOptionItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_FolderId",
                table: "Resources",
                column: "FolderId");

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
                name: "IX_Tasks_UnitGroupId",
                table: "Tasks",
                column: "UnitGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChoiceRequirements");

            migrationBuilder.DropTable(
                name: "ConditionVariableRequirement");

            migrationBuilder.DropTable(
                name: "NumericGroupResourceAssignments");

            migrationBuilder.DropTable(
                name: "NumericRequirements");

            migrationBuilder.DropTable(
                name: "OptionResourceAssignments");

            migrationBuilder.DropTable(
                name: "ResourceProperty");

            migrationBuilder.DropTable(
                name: "ResourceRequirements");

            migrationBuilder.DropTable(
                name: "TaskResourceAssignments");

            migrationBuilder.DropTable(
                name: "NumericInputGroups");

            migrationBuilder.DropTable(
                name: "ConditionResourceAssignments");

            migrationBuilder.DropTable(
                name: "OptionItems");

            migrationBuilder.DropTable(
                name: "ResourceProperties");

            migrationBuilder.DropTable(
                name: "ResourceOptionItems");

            migrationBuilder.DropTable(
                name: "QuestionConditions");

            migrationBuilder.DropTable(
                name: "OptionGroups");

            migrationBuilder.DropTable(
                name: "ResourcePropertySets");

            migrationBuilder.DropTable(
                name: "ResourceOptionGroups");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "Folders");

            migrationBuilder.DropTable(
                name: "ActionTypes");

            migrationBuilder.DropTable(
                name: "Actions");

            migrationBuilder.DropTable(
                name: "Falls");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "UnitGroups");
        }
    }
}