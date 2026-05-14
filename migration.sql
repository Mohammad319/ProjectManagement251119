IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE SEQUENCE [OrderSeq] AS int START WITH 0 INCREMENT BY 100 NO CYCLE;

CREATE TABLE [AccountGroups] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(80) NOT NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_AccountGroups] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_AccountGroups_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0)
);

CREATE TABLE [Accounts] (
    [Id] int NOT NULL IDENTITY,
    [Code] nvarchar(20) NOT NULL,
    [Name] nvarchar(80) NOT NULL,
    [IsVisible] bit NOT NULL,
    [AccountGroupId] int NOT NULL,
    [Metadata] nvarchar(max) NOT NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Accounts] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Accounts_Code_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Code]))) > 0),
    CONSTRAINT [CK_Accounts_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [FK_Accounts_AccountGroups_AccountGroupId] FOREIGN KEY ([AccountGroupId]) REFERENCES [AccountGroups] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Applications] (
    [Id] int NOT NULL IDENTITY,
    [Data] nvarchar(max) NOT NULL,
    [DepartmentId] int NOT NULL,
    [TenantId] int NOT NULL,
    [Name] nvarchar(80) NOT NULL,
    [IsVisible] bit NOT NULL,
    [UserId] int NOT NULL,
    [LastUpdate] datetime2 NOT NULL,
    CONSTRAINT [PK_Applications] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Applications_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0)
);

CREATE TABLE [ApplicationValues] (
    [Id] int NOT NULL IDENTITY,
    [CalculationId] int NOT NULL,
    [ApplicationId] int NOT NULL,
    [TenantId] int NOT NULL,
    [UserId] int NOT NULL,
    [Data] nvarchar(max) NOT NULL,
    [Name] nvarchar(80) NOT NULL,
    [Responsible] nvarchar(50) NOT NULL,
    [LastUpdate] datetime2 NOT NULL,
    CONSTRAINT [PK_ApplicationValues] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_ApplicationValues_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [FK_ApplicationValues_Applications_ApplicationId] FOREIGN KEY ([ApplicationId]) REFERENCES [Applications] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Calculations] (
    [Id] int NOT NULL IDENTITY,
    [Metadata] nvarchar(max) NOT NULL,
    [HourlyPrice] nvarchar(max) NOT NULL,
    [Factors] nvarchar(max) NOT NULL,
    [DepartmentId] int NOT NULL,
    [Code] nvarchar(20) NOT NULL,
    [Name] nvarchar(1000) NOT NULL,
    [Tax] int NOT NULL,
    [Procurement] int NOT NULL,
    [TenderDeadline] datetime2 NOT NULL,
    [TenderQA] datetime2 NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [SortOrder] int NOT NULL,
    [Sort] nvarchar(max) NOT NULL,
    [DisplayPresets] nvarchar(max) NOT NULL,
    [PublicationDate] datetime2 NULL,
    [DecisionDate] datetime2 NULL,
    [IsPrivate] bit NOT NULL,
    [IsVisible] bit NOT NULL,
    [OrganisationId] int NULL,
    [TypeId] int NULL,
    [StatusId] int NULL,
    [ProcurementMethodsId] int NULL,
    [CompensationId] int NULL,
    [ContractId] int NULL,
    [ProjectId] uniqueidentifier NOT NULL,
    [TemplateId] int NULL,
    [TemplateColumnId] int NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    [DeletedBy] int NULL,
    CONSTRAINT [PK_Calculations] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Calculations_Code_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Code]))) > 0),
    CONSTRAINT [CK_Calculations_DateRange] CHECK ([EndDate] >= [StartDate]),
    CONSTRAINT [CK_Calculations_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_Calculations_Tax_0_100] CHECK ([Tax] >= 0 AND [Tax] <= 100)
);

CREATE TABLE [TenderAttributeDefinitions] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(80) NOT NULL,
    [Note] nvarchar(3000) NULL,
    [CalculationId] int NOT NULL,
    [TenantId] int NOT NULL,
    CONSTRAINT [PK_TenderAttributeDefinitions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TenderAttributeDefinitions_Calculations_CalculationId] FOREIGN KEY ([CalculationId]) REFERENCES [Calculations] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Compensations] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    [Name] nvarchar(80) NOT NULL,
    [Color] nvarchar(7) NOT NULL,
    [SortOrder] int NOT NULL DEFAULT (NEXT VALUE FOR OrderSeq),
    [IsVisible] bit NOT NULL,
    CONSTRAINT [PK_Compensations] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Compensations_Color_Hex] CHECK ([Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'),
    CONSTRAINT [CK_Compensations_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_Compensations_SortOrder_NonNegative] CHECK ([SortOrder] >= 0)
);

CREATE TABLE [Contracts] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    [Name] nvarchar(80) NOT NULL,
    [Color] nvarchar(7) NOT NULL,
    [SortOrder] int NOT NULL DEFAULT (NEXT VALUE FOR OrderSeq),
    [IsVisible] bit NOT NULL,
    CONSTRAINT [PK_Contracts] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Contracts_Color_Hex] CHECK ([Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'),
    CONSTRAINT [CK_Contracts_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_Contracts_SortOrder_NonNegative] CHECK ([SortOrder] >= 0)
);

CREATE TABLE [Departments] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(80) NOT NULL,
    [Description] nvarchar(3000) NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Departments_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0)
);

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [ExternalAuthId] nvarchar(200) NOT NULL,
    [Email] nvarchar(254) NOT NULL,
    [UserName] nvarchar(100) NOT NULL,
    [DepartmentId] int NULL,
    [FirstName] nvarchar(80) NULL,
    [LastName] nvarchar(80) NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Users_Email_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Email]))) > 0),
    CONSTRAINT [CK_Users_UserName_NotEmpty] CHECK (LEN(LTRIM(RTRIM([UserName]))) > 0),
    CONSTRAINT [FK_Users_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Users_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Users_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Folders] (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(80) NOT NULL,
    [Color] nvarchar(7) NOT NULL,
    [SortOrder] int NOT NULL,
    [IsVisible] bit NOT NULL,
    [DepartmentId] int NOT NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Folders] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Folders_Department_Positive] CHECK ([DepartmentId] > 0),
    CONSTRAINT [CK_Folders_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_Folders_SortOrder_NonNegative] CHECK ([SortOrder] >= 0),
    CONSTRAINT [FK_Folders_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Folders_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Folders_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Opportunities] (
    [Id] int NOT NULL IDENTITY,
    [OpportunitiesRisks] nvarchar(3000) NOT NULL,
    [OpportunityType] nvarchar(3000) NULL,
    [CalculationId] int NOT NULL,
    [Metadata] nvarchar(max) NOT NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Opportunities] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Opportunities_Risks_NotEmpty] CHECK (LEN(LTRIM(RTRIM([OpportunitiesRisks]))) > 0),
    CONSTRAINT [FK_Opportunities_Calculations_CalculationId] FOREIGN KEY ([CalculationId]) REFERENCES [Calculations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Opportunities_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Opportunities_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OrganisationCategories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(80) NOT NULL,
    [ParentCategoryId] int NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_OrganisationCategories] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_OrganisationCategories_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_OrganisationCategories_Parent_Positive] CHECK ([ParentCategoryId] IS NULL OR [ParentCategoryId] > 0),
    CONSTRAINT [FK_OrganisationCategories_OrganisationCategories_ParentCategoryId] FOREIGN KEY ([ParentCategoryId]) REFERENCES [OrganisationCategories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OrganisationCategories_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OrganisationCategories_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [OrganisationTypes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(80) NOT NULL,
    [IsVisible] bit NOT NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_OrganisationTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_OrganisationTypes_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [FK_OrganisationTypes_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_OrganisationTypes_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ProcurementMethods] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    [Name] nvarchar(80) NOT NULL,
    [Color] nvarchar(7) NOT NULL,
    [SortOrder] int NOT NULL DEFAULT (NEXT VALUE FOR OrderSeq),
    [IsVisible] bit NOT NULL,
    CONSTRAINT [PK_ProcurementMethods] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_ProcurementMethods_Color_Hex] CHECK ([Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'),
    CONSTRAINT [CK_ProcurementMethods_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_ProcurementMethods_SortOrder_NonNegative] CHECK ([SortOrder] >= 0),
    CONSTRAINT [FK_ProcurementMethods_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ProcurementMethods_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ProjectTypes] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    [Name] nvarchar(80) NOT NULL,
    [Color] nvarchar(7) NOT NULL,
    [SortOrder] int NOT NULL DEFAULT (NEXT VALUE FOR OrderSeq),
    [IsVisible] bit NOT NULL,
    CONSTRAINT [PK_ProjectTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_ProjectTypes_Color_Hex] CHECK ([Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'),
    CONSTRAINT [CK_ProjectTypes_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_ProjectTypes_SortOrder_NonNegative] CHECK ([SortOrder] >= 0),
    CONSTRAINT [FK_ProjectTypes_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ProjectTypes_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ResourceTypes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(80) NOT NULL,
    [SortOrder] int NOT NULL DEFAULT (NEXT VALUE FOR OrderSeq),
    [IsVisible] bit NOT NULL,
    [Kind] int NOT NULL,
    [AccountId] int NULL,
    [Metadata] nvarchar(max) NOT NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_ResourceTypes] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_ResourceTypes_Account_Positive] CHECK ([AccountId] IS NULL OR [AccountId] > 0),
    CONSTRAINT [CK_ResourceTypes_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_ResourceTypes_SortOrder_NonNegative] CHECK ([SortOrder] >= 0),
    CONSTRAINT [FK_ResourceTypes_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]),
    CONSTRAINT [FK_ResourceTypes_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ResourceTypes_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ShareCalcs] (
    [Id] int NOT NULL IDENTITY,
    [DepartmentId] int NOT NULL,
    [CalculationId] int NOT NULL,
    [CreatedAtUserId] int NULL,
    [Metadata] nvarchar(max) NOT NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_ShareCalcs] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ShareCalcs_Calculations_CalculationId] FOREIGN KEY ([CalculationId]) REFERENCES [Calculations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ShareCalcs_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ShareCalcs_Users_CreatedAtUserId] FOREIGN KEY ([CreatedAtUserId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_ShareCalcs_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ShareCalcs_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Statuses] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    [Name] nvarchar(80) NOT NULL,
    [Color] nvarchar(7) NOT NULL,
    [SortOrder] int NOT NULL DEFAULT (NEXT VALUE FOR OrderSeq),
    [IsVisible] bit NOT NULL,
    CONSTRAINT [PK_Statuses] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Statuses_Color_Hex] CHECK ([Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'),
    CONSTRAINT [CK_Statuses_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_Statuses_SortOrder_NonNegative] CHECK ([SortOrder] >= 0),
    CONSTRAINT [FK_Statuses_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Statuses_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [StatusResources] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    [Name] nvarchar(80) NOT NULL,
    [Color] nvarchar(7) NOT NULL,
    [SortOrder] int NOT NULL DEFAULT (NEXT VALUE FOR OrderSeq),
    [IsVisible] bit NOT NULL,
    CONSTRAINT [PK_StatusResources] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_StatusResources_Color_Hex] CHECK ([Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'),
    CONSTRAINT [CK_StatusResources_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_StatusResources_SortOrder_NonNegative] CHECK ([SortOrder] >= 0),
    CONSTRAINT [FK_StatusResources_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StatusResources_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Storages] (
    [Id] int NOT NULL IDENTITY,
    [StorageValue] nvarchar(4000) NOT NULL,
    [StorageType] int NOT NULL,
    [StorageSort] int NOT NULL,
    [StorageLevel] int NOT NULL,
    [DepartmentId] int NOT NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Storages] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Storages_Value_NotEmpty] CHECK (LEN(LTRIM(RTRIM([StorageValue]))) > 0),
    CONSTRAINT [FK_Storages_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Storages_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Storages_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TaskStatuses] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    [Name] nvarchar(80) NOT NULL,
    [Color] nvarchar(7) NOT NULL,
    [SortOrder] int NOT NULL DEFAULT (NEXT VALUE FOR OrderSeq),
    [IsVisible] bit NOT NULL,
    CONSTRAINT [PK_TaskStatuses] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_TaskStatuses_Color_Hex] CHECK ([Color] LIKE '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'),
    CONSTRAINT [CK_TaskStatuses_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_TaskStatuses_SortOrder_NonNegative] CHECK ([SortOrder] >= 0),
    CONSTRAINT [FK_TaskStatuses_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TaskStatuses_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TemplateColumns] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(80) NOT NULL,
    [IsVisible] bit NOT NULL,
    [DepartmentId] int NULL,
    [Columns] nvarchar(max) NOT NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TemplateColumns] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_TemplateColumns_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [FK_TemplateColumns_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TemplateColumns_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TemplateColumns_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Templates] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(80) NOT NULL,
    [Metadata] nvarchar(max) NOT NULL,
    [IsVisible] bit NOT NULL,
    [DepartmentId] int NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Templates] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Templates_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [FK_Templates_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Templates_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Templates_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Organisations] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(80) NOT NULL,
    [Metadata] nvarchar(max) NOT NULL,
    [OrganisationCategoryId] int NOT NULL,
    [OrganisationTypeId] int NULL,
    [IsVisible] bit NOT NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Organisations] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Organisations_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [FK_Organisations_OrganisationCategories_OrganisationCategoryId] FOREIGN KEY ([OrganisationCategoryId]) REFERENCES [OrganisationCategories] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Organisations_OrganisationTypes_OrganisationTypeId] FOREIGN KEY ([OrganisationTypeId]) REFERENCES [OrganisationTypes] ([Id]),
    CONSTRAINT [FK_Organisations_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Organisations_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [ResourceSorts] (
    [Id] int NOT NULL IDENTITY,
    [Metadata] nvarchar(max) NOT NULL,
    [Name] nvarchar(80) NOT NULL,
    [IsVisible] bit NOT NULL,
    [SortOrder] int NOT NULL,
    [ResourceTypeId] int NOT NULL,
    [AccountId] int NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_ResourceSorts] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_ResourceSorts_Account_Positive] CHECK ([AccountId] IS NULL OR [AccountId] > 0),
    CONSTRAINT [CK_ResourceSorts_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_ResourceSorts_SortOrder_NonNegative] CHECK ([SortOrder] >= 0),
    CONSTRAINT [FK_ResourceSorts_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]),
    CONSTRAINT [FK_ResourceSorts_ResourceTypes_ResourceTypeId] FOREIGN KEY ([ResourceTypeId]) REFERENCES [ResourceTypes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ResourceSorts_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ResourceSorts_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Tasks] (
    [Id] int NOT NULL IDENTITY,
    [Metadata] nvarchar(max) NOT NULL,
    [Name] nvarchar(2000) NOT NULL,
    [NormalizedTextSv] nvarchar(450) NOT NULL,
    [IsActive] AS CAST(CASE LOWER(JSON_VALUE([Metadata], '$.IsActive')) WHEN 'true' THEN 1 WHEN '1' THEN 1 WHEN 'false' THEN 0 WHEN '0' THEN 0 ELSE 1 END AS bit) PERSISTED,
    [Unit] AS CAST(NULLIF(LTRIM(RTRIM(JSON_VALUE([Metadata], '$.Unit'))), '') AS nvarchar(30)) PERSISTED,
    [Type] AS COALESCE(TRY_CONVERT(int, JSON_VALUE([Metadata], '$.Type')), 0) PERSISTED,
    [SortOrder] int NOT NULL,
    [Note] AS CAST(NULLIF(LTRIM(RTRIM(JSON_VALUE([Metadata], '$.Note'))), '') AS nvarchar(3000)) PERSISTED,
    [Quantity] AS TRY_CONVERT(decimal(18,3), JSON_VALUE([Metadata], '$.Quantity')) PERSISTED,
    [Code] AS CAST(NULLIF(LTRIM(RTRIM(JSON_VALUE([Metadata], '$.Code'))), '') AS nvarchar(20)) PERSISTED,
    [IsOH] AS CAST(CASE LOWER(JSON_VALUE([Metadata], '$.IsOH')) WHEN 'true' THEN 1 WHEN '1' THEN 1 WHEN 'false' THEN 0 WHEN '0' THEN 0 ELSE 0 END AS bit) PERSISTED,
    [ParentTaskId] int NULL,
    [OpportunityId] int NULL,
    [CalculationId] int NOT NULL,
    [StatusId] int NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Tasks] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Tasks_Calculation_Positive] CHECK ([CalculationId] > 0),
    CONSTRAINT [CK_Tasks_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_Tasks_SortOrder_NonNegative] CHECK ([SortOrder] >= 0),
    CONSTRAINT [FK_Tasks_Calculations_CalculationId] FOREIGN KEY ([CalculationId]) REFERENCES [Calculations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Tasks_Opportunities_OpportunityId] FOREIGN KEY ([OpportunityId]) REFERENCES [Opportunities] ([Id]),
    CONSTRAINT [FK_Tasks_TaskStatuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [TaskStatuses] ([Id]),
    CONSTRAINT [FK_Tasks_Tasks_ParentTaskId] FOREIGN KEY ([ParentTaskId]) REFERENCES [Tasks] ([Id]),
    CONSTRAINT [FK_Tasks_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Tasks_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Projects] (
    [Id] uniqueidentifier NOT NULL,
    [Priority] int NOT NULL,
    [Name] nvarchar(1000) NOT NULL,
    [Code] nvarchar(20) NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    [TenderDeadline] datetime2 NOT NULL,
    [TenderQA] datetime2 NOT NULL,
    [SortOrder] int NOT NULL,
    [Metadata] nvarchar(max) NOT NULL,
    [ProjectTypeId] int NULL,
    [FolderId] uniqueidentifier NOT NULL,
    [OrganisationId] int NULL,
    [ProcurementMethodId] int NULL,
    [CompensationId] int NULL,
    [ContractId] int NULL,
    [IsVisible] bit NOT NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    [IsDeleted] bit NOT NULL,
    [DeletedAt] datetime2 NULL,
    [DeletedBy] int NULL,
    CONSTRAINT [PK_Projects] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Projects_DateRange] CHECK ([EndDate] >= [StartDate]),
    CONSTRAINT [CK_Projects_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_Projects_SortOrder_NonNegative] CHECK ([SortOrder] >= 0),
    CONSTRAINT [FK_Projects_Compensations_CompensationId] FOREIGN KEY ([CompensationId]) REFERENCES [Compensations] ([Id]),
    CONSTRAINT [FK_Projects_Contracts_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [Contracts] ([Id]),
    CONSTRAINT [FK_Projects_Folders_FolderId] FOREIGN KEY ([FolderId]) REFERENCES [Folders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Projects_Organisations_OrganisationId] FOREIGN KEY ([OrganisationId]) REFERENCES [Organisations] ([Id]),
    CONSTRAINT [FK_Projects_ProcurementMethods_ProcurementMethodId] FOREIGN KEY ([ProcurementMethodId]) REFERENCES [ProcurementMethods] ([Id]),
    CONSTRAINT [FK_Projects_ProjectTypes_ProjectTypeId] FOREIGN KEY ([ProjectTypeId]) REFERENCES [ProjectTypes] ([Id]),
    CONSTRAINT [FK_Projects_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Projects_Users_DeletedBy] FOREIGN KEY ([DeletedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Projects_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Tenders] (
    [Id] int NOT NULL IDENTITY,
    [Attributes] nvarchar(max) NULL,
    [Note] nvarchar(3000) NULL,
    [CalculationId] int NOT NULL,
    [OrganisationId] int NOT NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Tenders] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tenders_Calculations_CalculationId] FOREIGN KEY ([CalculationId]) REFERENCES [Calculations] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Tenders_Organisations_OrganisationId] FOREIGN KEY ([OrganisationId]) REFERENCES [Organisations] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Tenders_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Tenders_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Resources] (
    [Id] int NOT NULL IDENTITY,
    [Metadata] nvarchar(max) NOT NULL,
    [Name] nvarchar(1000) NOT NULL,
    [IsActive] bit NOT NULL,
    [Unit] AS CAST(NULLIF(LTRIM(RTRIM(JSON_VALUE([Metadata], '$.Unit'))), '') AS nvarchar(30)) PERSISTED,
    [ResType] int NOT NULL,
    [SortOrder] int NOT NULL,
    [Note] AS CAST(NULLIF(LTRIM(RTRIM(JSON_VALUE([Metadata], '$.Note'))), '') AS nvarchar(3000)) PERSISTED,
    [Quantity] AS TRY_CONVERT(decimal(18,3), JSON_VALUE([Metadata], '$.Quantity')) PERSISTED,
    [TaskId] int NOT NULL,
    [OpportunityId] int NULL,
    [AccountId] int NULL,
    [StatusId] int NULL,
    [ResourceSortId] int NULL,
    [ResourceTypeId] int NULL,
    [PrimaryOfferId] int NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Resources] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Resources_Name_NotEmpty] CHECK (LEN(LTRIM(RTRIM([Name]))) > 0),
    CONSTRAINT [CK_Resources_SortOrder_NonNegative] CHECK ([SortOrder] >= 0),
    CONSTRAINT [CK_Resources_Task_Positive] CHECK ([TaskId] > 0),
    CONSTRAINT [FK_Resources_Accounts_AccountId] FOREIGN KEY ([AccountId]) REFERENCES [Accounts] ([Id]),
    CONSTRAINT [FK_Resources_Opportunities_OpportunityId] FOREIGN KEY ([OpportunityId]) REFERENCES [Opportunities] ([Id]),
    CONSTRAINT [FK_Resources_ResourceSorts_ResourceSortId] FOREIGN KEY ([ResourceSortId]) REFERENCES [ResourceSorts] ([Id]),
    CONSTRAINT [FK_Resources_ResourceTypes_ResourceTypeId] FOREIGN KEY ([ResourceTypeId]) REFERENCES [ResourceTypes] ([Id]),
    CONSTRAINT [FK_Resources_StatusResources_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [StatusResources] ([Id]),
    CONSTRAINT [FK_Resources_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Resources_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Resources_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [TenderAttributeBinds] (
    [Id] int NOT NULL IDENTITY,
    [TenderId] int NOT NULL,
    [TenderAttributeId] int NOT NULL,
    [Value] decimal(18,2) NOT NULL,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_TenderAttributeBinds] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TenderAttributeBinds_TenderAttributeDefinitions_TenderAttributeId] FOREIGN KEY ([TenderAttributeId]) REFERENCES [TenderAttributeDefinitions] ([Id]),
    CONSTRAINT [FK_TenderAttributeBinds_Tenders_TenderId] FOREIGN KEY ([TenderId]) REFERENCES [Tenders] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TenderAttributeBinds_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_TenderAttributeBinds_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Offers] (
    [Id] int NOT NULL IDENTITY,
    [Date] datetime2 NOT NULL,
    [Comment] nvarchar(3000) NULL,
    [Metadata] nvarchar(max) NOT NULL,
    [OrganisationId] int NULL,
    [ResourceId] int NOT NULL,
    [BaseCostValue] AS TRY_CONVERT(decimal(18,2), JSON_VALUE([Metadata], '$.BaseCost')) PERSISTED,
    [CostValue] AS TRY_CONVERT(decimal(18,2), JSON_VALUE([Metadata], '$.Cost')) PERSISTED,
    [TenantId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedAt] datetime2 NULL,
    [UpdatedBy] int NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Offers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Offers_Organisations_OrganisationId] FOREIGN KEY ([OrganisationId]) REFERENCES [Organisations] ([Id]),
    CONSTRAINT [FK_Offers_Resources_ResourceId] FOREIGN KEY ([ResourceId]) REFERENCES [Resources] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Offers_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Offers_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_AccountGroups_CreatedBy] ON [AccountGroups] ([CreatedBy]);

CREATE INDEX [IX_AccountGroups_UpdatedBy] ON [AccountGroups] ([UpdatedBy]);

CREATE UNIQUE INDEX [UX_AccountGroups_Tenant_Name] ON [AccountGroups] ([TenantId], [Name]);

CREATE INDEX [IX_Accounts_AccountGroupId] ON [Accounts] ([AccountGroupId]);

CREATE INDEX [IX_Accounts_CreatedBy] ON [Accounts] ([CreatedBy]);

CREATE INDEX [IX_Accounts_Tenant_Group_Visible_Id] ON [Accounts] ([TenantId], [AccountGroupId], [IsVisible], [Id]);

CREATE INDEX [IX_Accounts_Tenant_Name] ON [Accounts] ([TenantId], [Name]);

CREATE INDEX [IX_Accounts_UpdatedBy] ON [Accounts] ([UpdatedBy]);

CREATE UNIQUE INDEX [UX_Accounts_Tenant_Code] ON [Accounts] ([TenantId], [Code]);

CREATE INDEX [IX_Applications_DepartmentId] ON [Applications] ([DepartmentId]);

CREATE INDEX [IX_Applications_Tenant_Department_Visible_Id] ON [Applications] ([TenantId], [DepartmentId], [IsVisible], [Id]);

CREATE INDEX [IX_Applications_Tenant_User] ON [Applications] ([TenantId], [UserId]);

CREATE INDEX [IX_ApplicationValues_ApplicationId] ON [ApplicationValues] ([ApplicationId]);

CREATE INDEX [IX_ApplicationValues_CalculationId] ON [ApplicationValues] ([CalculationId]);

CREATE INDEX [IX_ApplicationValues_Tenant_App] ON [ApplicationValues] ([TenantId], [ApplicationId]);

CREATE INDEX [IX_ApplicationValues_Tenant_Calc_LastUpdate] ON [ApplicationValues] ([TenantId], [CalculationId], [LastUpdate]);

CREATE INDEX [IX_ApplicationValues_Tenant_User] ON [ApplicationValues] ([TenantId], [UserId]);

CREATE UNIQUE INDEX [UX_ApplicationValues_Tenant_Calc_App] ON [ApplicationValues] ([TenantId], [CalculationId], [ApplicationId]);

CREATE INDEX [IX_Calculations_CompensationId] ON [Calculations] ([CompensationId]);

CREATE INDEX [IX_Calculations_ContractId] ON [Calculations] ([ContractId]);

CREATE INDEX [IX_Calculations_CreatedBy] ON [Calculations] ([CreatedBy]);

CREATE INDEX [IX_Calculations_DeletedBy] ON [Calculations] ([DeletedBy]);

CREATE INDEX [IX_Calculations_OrganisationId] ON [Calculations] ([OrganisationId]);

CREATE INDEX [IX_Calculations_ProcurementMethodsId] ON [Calculations] ([ProcurementMethodsId]);

CREATE INDEX [IX_Calculations_ProjectId] ON [Calculations] ([ProjectId]);

CREATE INDEX [IX_Calculations_StatusId] ON [Calculations] ([StatusId]);

CREATE INDEX [IX_Calculations_TemplateColumnId] ON [Calculations] ([TemplateColumnId]);

CREATE INDEX [IX_Calculations_TemplateId] ON [Calculations] ([TemplateId]);

CREATE INDEX [IX_Calculations_Tenant_Department] ON [Calculations] ([TenantId], [DepartmentId]);

CREATE INDEX [IX_Calculations_Tenant_Id_Department] ON [Calculations] ([TenantId], [Id], [DepartmentId]);

CREATE INDEX [IX_Calculations_Tenant_Project] ON [Calculations] ([TenantId], [ProjectId]);

CREATE INDEX [IX_Calculations_Tenant_Project_Department_Order] ON [Calculations] ([TenantId], [ProjectId], [DepartmentId], [SortOrder]);

CREATE INDEX [IX_Calculations_Tenant_Project_Private_Order] ON [Calculations] ([TenantId], [ProjectId], [IsPrivate], [SortOrder]);

CREATE INDEX [IX_Calculations_Tenant_Status] ON [Calculations] ([TenantId], [StatusId]);

CREATE INDEX [IX_Calculations_TypeId] ON [Calculations] ([TypeId]);

CREATE INDEX [IX_Calculations_UpdatedBy] ON [Calculations] ([UpdatedBy]);

CREATE UNIQUE INDEX [UX_Calculations_Tenant_Project_Code] ON [Calculations] ([TenantId], [ProjectId], [Code]) WHERE [IsDeleted] = 0;

CREATE INDEX [IX_Compensations_CreatedBy] ON [Compensations] ([CreatedBy]);

CREATE INDEX [IX_Compensations_Tenant_Name] ON [Compensations] ([TenantId], [Name]);

CREATE INDEX [IX_Compensations_Tenant_Visible_Order] ON [Compensations] ([TenantId], [IsVisible], [SortOrder]);

CREATE INDEX [IX_Compensations_UpdatedBy] ON [Compensations] ([UpdatedBy]);

CREATE INDEX [IX_Contracts_CreatedBy] ON [Contracts] ([CreatedBy]);

CREATE INDEX [IX_Contracts_Tenant_Name] ON [Contracts] ([TenantId], [Name]);

CREATE INDEX [IX_Contracts_Tenant_Visible_Order] ON [Contracts] ([TenantId], [IsVisible], [SortOrder]);

CREATE INDEX [IX_Contracts_UpdatedBy] ON [Contracts] ([UpdatedBy]);

CREATE INDEX [IX_Departments_CreatedBy] ON [Departments] ([CreatedBy]);

CREATE INDEX [IX_Departments_UpdatedBy] ON [Departments] ([UpdatedBy]);

CREATE UNIQUE INDEX [UX_Departments_Tenant_Name] ON [Departments] ([TenantId], [Name]);

CREATE INDEX [IX_Folders_CreatedBy] ON [Folders] ([CreatedBy]);

CREATE INDEX [IX_Folders_DepartmentId] ON [Folders] ([DepartmentId]);

CREATE INDEX [IX_Folders_Tenant_Department_Name] ON [Folders] ([TenantId], [DepartmentId], [Name]);

CREATE INDEX [IX_Folders_Tenant_Department_Visible_Order] ON [Folders] ([TenantId], [DepartmentId], [IsVisible], [SortOrder]);

CREATE INDEX [IX_Folders_UpdatedBy] ON [Folders] ([UpdatedBy]);

CREATE INDEX [IX_Offers_CreatedBy] ON [Offers] ([CreatedBy]);

CREATE INDEX [IX_Offers_OrganisationId] ON [Offers] ([OrganisationId]);

CREATE INDEX [IX_Offers_ResourceId] ON [Offers] ([ResourceId]);

CREATE INDEX [IX_Offers_Tenant_BaseCostValue] ON [Offers] ([TenantId], [BaseCostValue]);

CREATE INDEX [IX_Offers_Tenant_CostValue] ON [Offers] ([TenantId], [CostValue]);

CREATE INDEX [IX_Offers_Tenant_Org_Date] ON [Offers] ([TenantId], [OrganisationId], [Date]);

CREATE INDEX [IX_Offers_TenantId_ResourceId] ON [Offers] ([TenantId], [ResourceId]);

CREATE INDEX [IX_Offers_UpdatedBy] ON [Offers] ([UpdatedBy]);

CREATE INDEX [IX_Opportunities_CalculationId] ON [Opportunities] ([CalculationId]);

CREATE INDEX [IX_Opportunities_CreatedBy] ON [Opportunities] ([CreatedBy]);

CREATE INDEX [IX_Opportunities_Tenant_Calc] ON [Opportunities] ([TenantId], [CalculationId]);

CREATE INDEX [IX_Opportunities_UpdatedBy] ON [Opportunities] ([UpdatedBy]);

CREATE INDEX [IX_OrganisationCategories_CreatedBy] ON [OrganisationCategories] ([CreatedBy]);

CREATE INDEX [IX_OrganisationCategories_ParentCategoryId] ON [OrganisationCategories] ([ParentCategoryId]);

CREATE INDEX [IX_OrganisationCategories_Tenant_Parent] ON [OrganisationCategories] ([TenantId], [ParentCategoryId]);

CREATE INDEX [IX_OrganisationCategories_UpdatedBy] ON [OrganisationCategories] ([UpdatedBy]);

CREATE UNIQUE INDEX [UX_OrganisationCategories_Tenant_Name] ON [OrganisationCategories] ([TenantId], [Name]);

CREATE INDEX [IX_Organisations_CreatedBy] ON [Organisations] ([CreatedBy]);

CREATE INDEX [IX_Organisations_OrganisationCategoryId] ON [Organisations] ([OrganisationCategoryId]);

CREATE INDEX [IX_Organisations_OrganisationTypeId] ON [Organisations] ([OrganisationTypeId]);

CREATE INDEX [IX_Organisations_Tenant_Category] ON [Organisations] ([TenantId], [OrganisationCategoryId]);

CREATE INDEX [IX_Organisations_Tenant_Visible_Name] ON [Organisations] ([TenantId], [IsVisible], [Name]);

CREATE INDEX [IX_Organisations_UpdatedBy] ON [Organisations] ([UpdatedBy]);

CREATE INDEX [IX_OrganisationTypes_CreatedBy] ON [OrganisationTypes] ([CreatedBy]);

CREATE INDEX [IX_OrganisationTypes_UpdatedBy] ON [OrganisationTypes] ([UpdatedBy]);

CREATE UNIQUE INDEX [UX_OrganisationTypes_Tenant_Name] ON [OrganisationTypes] ([TenantId], [Name]);

CREATE INDEX [IX_ProcurementMethods_CreatedBy] ON [ProcurementMethods] ([CreatedBy]);

CREATE INDEX [IX_ProcurementMethods_Tenant_Name] ON [ProcurementMethods] ([TenantId], [Name]);

CREATE INDEX [IX_ProcurementMethods_Tenant_Visible_Order] ON [ProcurementMethods] ([TenantId], [IsVisible], [SortOrder]);

CREATE INDEX [IX_ProcurementMethods_UpdatedBy] ON [ProcurementMethods] ([UpdatedBy]);

CREATE INDEX [IX_Projects_CompensationId] ON [Projects] ([CompensationId]);

CREATE INDEX [IX_Projects_ContractId] ON [Projects] ([ContractId]);

CREATE INDEX [IX_Projects_CreatedBy] ON [Projects] ([CreatedBy]);

CREATE INDEX [IX_Projects_DeletedBy] ON [Projects] ([DeletedBy]);

CREATE INDEX [IX_Projects_FolderId] ON [Projects] ([FolderId]);

CREATE INDEX [IX_Projects_OrganisationId] ON [Projects] ([OrganisationId]);

CREATE INDEX [IX_Projects_ProcurementMethodId] ON [Projects] ([ProcurementMethodId]);

CREATE INDEX [IX_Projects_ProjectTypeId] ON [Projects] ([ProjectTypeId]);

CREATE INDEX [IX_Projects_Tenant_CreatedBy] ON [Projects] ([TenantId], [CreatedBy]);

CREATE INDEX [IX_Projects_Tenant_Folder_Visible_Order] ON [Projects] ([TenantId], [FolderId], [IsVisible], [SortOrder]);

CREATE INDEX [IX_Projects_Tenant_Name] ON [Projects] ([TenantId], [Name]);

CREATE INDEX [IX_Projects_UpdatedBy] ON [Projects] ([UpdatedBy]);

CREATE UNIQUE INDEX [UX_Projects_Tenant_Code] ON [Projects] ([TenantId], [Code]) WHERE [IsDeleted] = 0 AND [Code] IS NOT NULL AND [Code] <> '';

CREATE INDEX [IX_ProjectTypes_CreatedBy] ON [ProjectTypes] ([CreatedBy]);

CREATE INDEX [IX_ProjectTypes_Tenant_Name] ON [ProjectTypes] ([TenantId], [Name]);

CREATE INDEX [IX_ProjectTypes_Tenant_Visible_Order] ON [ProjectTypes] ([TenantId], [IsVisible], [SortOrder]);

CREATE INDEX [IX_ProjectTypes_UpdatedBy] ON [ProjectTypes] ([UpdatedBy]);

CREATE INDEX [IX_Resources_AccountId] ON [Resources] ([AccountId]);

CREATE INDEX [IX_Resources_CreatedBy] ON [Resources] ([CreatedBy]);

CREATE INDEX [IX_Resources_OpportunityId] ON [Resources] ([OpportunityId]);

CREATE INDEX [IX_Resources_ResourceSortId] ON [Resources] ([ResourceSortId]);

CREATE INDEX [IX_Resources_ResourceTypeId] ON [Resources] ([ResourceTypeId]);

CREATE INDEX [IX_Resources_StatusId] ON [Resources] ([StatusId]);

CREATE INDEX [IX_Resources_TaskId] ON [Resources] ([TaskId]);

CREATE INDEX [IX_Resources_Tenant_Account] ON [Resources] ([TenantId], [AccountId]);

CREATE INDEX [IX_Resources_Tenant_Opportunity] ON [Resources] ([TenantId], [OpportunityId]);

CREATE INDEX [IX_Resources_Tenant_ResourceSort] ON [Resources] ([TenantId], [ResourceSortId]);

CREATE INDEX [IX_Resources_Tenant_ResourceType] ON [Resources] ([TenantId], [ResourceTypeId]);

CREATE INDEX [IX_Resources_Tenant_Status] ON [Resources] ([TenantId], [StatusId]);

CREATE INDEX [IX_Resources_Tenant_Task_Sort] ON [Resources] ([TenantId], [TaskId], [SortOrder]);

CREATE INDEX [IX_Resources_TenantId_TaskId] ON [Resources] ([TenantId], [TaskId]);

CREATE INDEX [IX_Resources_UpdatedBy] ON [Resources] ([UpdatedBy]);

CREATE INDEX [IX_ResourceSorts_AccountId] ON [ResourceSorts] ([AccountId]);

CREATE INDEX [IX_ResourceSorts_CreatedBy] ON [ResourceSorts] ([CreatedBy]);

CREATE INDEX [IX_ResourceSorts_ResourceTypeId] ON [ResourceSorts] ([ResourceTypeId]);

CREATE INDEX [IX_ResourceSorts_Tenant_Type_Name] ON [ResourceSorts] ([TenantId], [ResourceTypeId], [Name]);

CREATE INDEX [IX_ResourceSorts_Tenant_Type_Visible_Order] ON [ResourceSorts] ([TenantId], [ResourceTypeId], [IsVisible], [SortOrder]);

CREATE INDEX [IX_ResourceSorts_TenantId_ResourceTypeId] ON [ResourceSorts] ([TenantId], [ResourceTypeId]);

CREATE INDEX [IX_ResourceSorts_UpdatedBy] ON [ResourceSorts] ([UpdatedBy]);

CREATE INDEX [IX_ResourceTypes_AccountId] ON [ResourceTypes] ([AccountId]);

CREATE INDEX [IX_ResourceTypes_CreatedBy] ON [ResourceTypes] ([CreatedBy]);

CREATE INDEX [IX_ResourceTypes_Tenant_Kind_Visible_Order] ON [ResourceTypes] ([TenantId], [Kind], [IsVisible], [SortOrder]);

CREATE INDEX [IX_ResourceTypes_Tenant_Name] ON [ResourceTypes] ([TenantId], [Name]);

CREATE INDEX [IX_ResourceTypes_TenantId_AccountId] ON [ResourceTypes] ([TenantId], [AccountId]);

CREATE INDEX [IX_ResourceTypes_UpdatedBy] ON [ResourceTypes] ([UpdatedBy]);

CREATE INDEX [IX_ShareCalcs_CalculationId] ON [ShareCalcs] ([CalculationId]);

CREATE INDEX [IX_ShareCalcs_CreatedAtUserId] ON [ShareCalcs] ([CreatedAtUserId]);

CREATE INDEX [IX_ShareCalcs_CreatedBy] ON [ShareCalcs] ([CreatedBy]);

CREATE INDEX [IX_ShareCalcs_DepartmentId] ON [ShareCalcs] ([DepartmentId]);

CREATE INDEX [IX_ShareCalcs_Tenant_Calc] ON [ShareCalcs] ([TenantId], [CalculationId]);

CREATE INDEX [IX_ShareCalcs_Tenant_Department] ON [ShareCalcs] ([TenantId], [DepartmentId]);

CREATE INDEX [IX_ShareCalcs_UpdatedBy] ON [ShareCalcs] ([UpdatedBy]);

CREATE UNIQUE INDEX [UX_ShareCalcs_Tenant_Calc_Department] ON [ShareCalcs] ([TenantId], [CalculationId], [DepartmentId]);

CREATE INDEX [IX_Statuses_CreatedBy] ON [Statuses] ([CreatedBy]);

CREATE INDEX [IX_Statuses_Tenant_Name] ON [Statuses] ([TenantId], [Name]);

CREATE INDEX [IX_Statuses_Tenant_Visible_Order] ON [Statuses] ([TenantId], [IsVisible], [SortOrder]);

CREATE INDEX [IX_Statuses_UpdatedBy] ON [Statuses] ([UpdatedBy]);

CREATE INDEX [IX_StatusResources_CreatedBy] ON [StatusResources] ([CreatedBy]);

CREATE INDEX [IX_StatusResources_Tenant_Name] ON [StatusResources] ([TenantId], [Name]);

CREATE INDEX [IX_StatusResources_Tenant_Visible_Order] ON [StatusResources] ([TenantId], [IsVisible], [SortOrder]);

CREATE INDEX [IX_StatusResources_UpdatedBy] ON [StatusResources] ([UpdatedBy]);

CREATE INDEX [IX_Storages_CreatedBy] ON [Storages] ([CreatedBy]);

CREATE INDEX [IX_Storages_DepartmentId] ON [Storages] ([DepartmentId]);

CREATE INDEX [IX_Storages_Tenant_Department_Type_Sort_Level] ON [Storages] ([TenantId], [DepartmentId], [StorageType], [StorageSort], [StorageLevel]);

CREATE INDEX [IX_Storages_UpdatedBy] ON [Storages] ([UpdatedBy]);

CREATE INDEX [IX_Tasks_CalculationId] ON [Tasks] ([CalculationId]);

CREATE INDEX [IX_Tasks_CreatedBy] ON [Tasks] ([CreatedBy]);

CREATE INDEX [IX_Tasks_OpportunityId] ON [Tasks] ([OpportunityId]);

CREATE INDEX [IX_Tasks_ParentTaskId] ON [Tasks] ([ParentTaskId]);

CREATE INDEX [IX_Tasks_StatusId] ON [Tasks] ([StatusId]);

CREATE INDEX [IX_Tasks_Tenant_Calc_Parent_Sort] ON [Tasks] ([TenantId], [CalculationId], [ParentTaskId], [SortOrder]);

CREATE INDEX [IX_Tasks_Tenant_Calc_Status] ON [Tasks] ([TenantId], [CalculationId], [StatusId]);

CREATE INDEX [IX_Tasks_Tenant_NormalizedTextSv] ON [Tasks] ([TenantId], [NormalizedTextSv]);

CREATE INDEX [IX_Tasks_Tenant_Parent] ON [Tasks] ([TenantId], [ParentTaskId]);

CREATE INDEX [IX_Tasks_TenantId_CalculationId] ON [Tasks] ([TenantId], [CalculationId]);

CREATE INDEX [IX_Tasks_UpdatedBy] ON [Tasks] ([UpdatedBy]);

CREATE INDEX [IX_TaskStatuses_CreatedBy] ON [TaskStatuses] ([CreatedBy]);

CREATE INDEX [IX_TaskStatuses_Tenant_Name] ON [TaskStatuses] ([TenantId], [Name]);

CREATE INDEX [IX_TaskStatuses_Tenant_Visible_Order] ON [TaskStatuses] ([TenantId], [IsVisible], [SortOrder]);

CREATE INDEX [IX_TaskStatuses_UpdatedBy] ON [TaskStatuses] ([UpdatedBy]);

CREATE INDEX [IX_TemplateColumns_CreatedBy] ON [TemplateColumns] ([CreatedBy]);

CREATE INDEX [IX_TemplateColumns_DepartmentId] ON [TemplateColumns] ([DepartmentId]);

CREATE INDEX [IX_TemplateColumns_Tenant_Department_Id] ON [TemplateColumns] ([TenantId], [DepartmentId], [Id]);

CREATE INDEX [IX_TemplateColumns_Tenant_Department_Name] ON [TemplateColumns] ([TenantId], [DepartmentId], [Name]);

CREATE INDEX [IX_TemplateColumns_UpdatedBy] ON [TemplateColumns] ([UpdatedBy]);

CREATE INDEX [IX_Templates_CreatedBy] ON [Templates] ([CreatedBy]);

CREATE INDEX [IX_Templates_DepartmentId] ON [Templates] ([DepartmentId]);

CREATE INDEX [IX_Templates_TenantId_DepartmentId_Id] ON [Templates] ([TenantId], [DepartmentId], [Id]);

CREATE INDEX [IX_Templates_TenantId_Name] ON [Templates] ([TenantId], [Name]);

CREATE INDEX [IX_Templates_UpdatedBy] ON [Templates] ([UpdatedBy]);

CREATE INDEX [IX_TenderAttributeBinds_CreatedBy] ON [TenderAttributeBinds] ([CreatedBy]);

CREATE INDEX [IX_TenderAttributeBinds_TenderAttributeId] ON [TenderAttributeBinds] ([TenderAttributeId]);

CREATE INDEX [IX_TenderAttributeBinds_TenderId] ON [TenderAttributeBinds] ([TenderId]);

CREATE INDEX [IX_TenderAttributeBinds_UpdatedBy] ON [TenderAttributeBinds] ([UpdatedBy]);

CREATE UNIQUE INDEX [UX_TenderAttributeBinds_Tenant_Tender_Attr] ON [TenderAttributeBinds] ([TenantId], [TenderId], [TenderAttributeId]);

CREATE INDEX [IX_TenderAttrDefs_Tenant_Calc] ON [TenderAttributeDefinitions] ([TenantId], [CalculationId]);

CREATE INDEX [IX_TenderAttributeDefinitions_CalculationId] ON [TenderAttributeDefinitions] ([CalculationId]);

CREATE INDEX [IX_Tenders_CalculationId] ON [Tenders] ([CalculationId]);

CREATE INDEX [IX_Tenders_CreatedBy] ON [Tenders] ([CreatedBy]);

CREATE INDEX [IX_Tenders_OrganisationId] ON [Tenders] ([OrganisationId]);

CREATE INDEX [IX_Tenders_Tenant_Calc] ON [Tenders] ([TenantId], [CalculationId]);

CREATE INDEX [IX_Tenders_Tenant_Org] ON [Tenders] ([TenantId], [OrganisationId]);

CREATE INDEX [IX_Tenders_UpdatedBy] ON [Tenders] ([UpdatedBy]);

CREATE INDEX [IX_Users_CreatedBy] ON [Users] ([CreatedBy]);

CREATE INDEX [IX_Users_DepartmentId] ON [Users] ([DepartmentId]);

CREATE INDEX [IX_Users_Tenant_UserName] ON [Users] ([TenantId], [UserName]);

CREATE INDEX [IX_Users_UpdatedBy] ON [Users] ([UpdatedBy]);

CREATE UNIQUE INDEX [UX_Users_Tenant_Email] ON [Users] ([TenantId], [Email]);

CREATE UNIQUE INDEX [UX_Users_Tenant_ExternalAuthId] ON [Users] ([TenantId], [ExternalAuthId]) WHERE [ExternalAuthId] IS NOT NULL AND [ExternalAuthId] <> '';

ALTER TABLE [AccountGroups] ADD CONSTRAINT [FK_AccountGroups_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [AccountGroups] ADD CONSTRAINT [FK_AccountGroups_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Accounts] ADD CONSTRAINT [FK_Accounts_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Accounts] ADD CONSTRAINT [FK_Accounts_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Applications] ADD CONSTRAINT [FK_Applications_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [ApplicationValues] ADD CONSTRAINT [FK_ApplicationValues_Calculations_CalculationId] FOREIGN KEY ([CalculationId]) REFERENCES [Calculations] ([Id]) ON DELETE CASCADE;

ALTER TABLE [Calculations] ADD CONSTRAINT [FK_Calculations_Compensations_CompensationId] FOREIGN KEY ([CompensationId]) REFERENCES [Compensations] ([Id]);

ALTER TABLE [Calculations] ADD CONSTRAINT [FK_Calculations_Contracts_ContractId] FOREIGN KEY ([ContractId]) REFERENCES [Contracts] ([Id]);

ALTER TABLE [Calculations] ADD CONSTRAINT [FK_Calculations_Organisations_OrganisationId] FOREIGN KEY ([OrganisationId]) REFERENCES [Organisations] ([Id]);

ALTER TABLE [Calculations] ADD CONSTRAINT [FK_Calculations_ProcurementMethods_ProcurementMethodsId] FOREIGN KEY ([ProcurementMethodsId]) REFERENCES [ProcurementMethods] ([Id]);

ALTER TABLE [Calculations] ADD CONSTRAINT [FK_Calculations_ProjectTypes_TypeId] FOREIGN KEY ([TypeId]) REFERENCES [ProjectTypes] ([Id]);

ALTER TABLE [Calculations] ADD CONSTRAINT [FK_Calculations_Projects_ProjectId] FOREIGN KEY ([ProjectId]) REFERENCES [Projects] ([Id]) ON DELETE CASCADE;

ALTER TABLE [Calculations] ADD CONSTRAINT [FK_Calculations_Statuses_StatusId] FOREIGN KEY ([StatusId]) REFERENCES [Statuses] ([Id]);

ALTER TABLE [Calculations] ADD CONSTRAINT [FK_Calculations_TemplateColumns_TemplateColumnId] FOREIGN KEY ([TemplateColumnId]) REFERENCES [TemplateColumns] ([Id]);

ALTER TABLE [Calculations] ADD CONSTRAINT [FK_Calculations_Templates_TemplateId] FOREIGN KEY ([TemplateId]) REFERENCES [Templates] ([Id]);

ALTER TABLE [Calculations] ADD CONSTRAINT [FK_Calculations_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Calculations] ADD CONSTRAINT [FK_Calculations_Users_DeletedBy] FOREIGN KEY ([DeletedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Calculations] ADD CONSTRAINT [FK_Calculations_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Compensations] ADD CONSTRAINT [FK_Compensations_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Compensations] ADD CONSTRAINT [FK_Compensations_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Contracts] ADD CONSTRAINT [FK_Contracts_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Contracts] ADD CONSTRAINT [FK_Contracts_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Departments] ADD CONSTRAINT [FK_Departments_Users_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Departments] ADD CONSTRAINT [FK_Departments_Users_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260501134929_DB260201', N'10.0.7');

COMMIT;
GO

BEGIN TRANSACTION;
DECLARE @var nvarchar(max);
SELECT @var = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tasks]') AND [c].[name] = N'Unit');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Tasks] DROP CONSTRAINT ' + @var + ';');
ALTER TABLE [Tasks] DROP COLUMN [Unit];
ALTER TABLE [Tasks] ADD [Unit] nvarchar(30) NULL;

DECLARE @var1 nvarchar(max);
SELECT @var1 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tasks]') AND [c].[name] = N'Quantity');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Tasks] DROP CONSTRAINT ' + @var1 + ';');
ALTER TABLE [Tasks] DROP COLUMN [Quantity];
ALTER TABLE [Tasks] ADD [Quantity] decimal(18,3) NULL;

DECLARE @var2 nvarchar(max);
SELECT @var2 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Resources]') AND [c].[name] = N'Unit');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Resources] DROP CONSTRAINT ' + @var2 + ';');
ALTER TABLE [Resources] DROP COLUMN [Unit];
ALTER TABLE [Resources] ADD [Unit] nvarchar(30) NULL;

DECLARE @var3 nvarchar(max);
SELECT @var3 = QUOTENAME([d].[name])
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Resources]') AND [c].[name] = N'Quantity');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Resources] DROP CONSTRAINT ' + @var3 + ';');
ALTER TABLE [Resources] DROP COLUMN [Quantity];
ALTER TABLE [Resources] ADD [Quantity] decimal(18,3) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260504131333_DB260201_2', N'10.0.7');

COMMIT;
GO

BEGIN TRANSACTION;
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260505063810_DB260201_3', N'10.0.7');

COMMIT;
GO

BEGIN TRANSACTION;
CREATE TABLE [PriceImportJobs] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] int NOT NULL,
    [SupplierName] nvarchar(300) NULL,
    [SourceFileName] nvarchar(500) NOT NULL,
    [SourceFilePath] nvarchar(1000) NOT NULL,
    [SourceFileHash] nvarchar(450) NULL,
    [FileType] int NOT NULL,
    [StartedAt] datetime2 NOT NULL,
    [CompletedAt] datetime2 NULL,
    [Status] int NOT NULL,
    [TotalCandidates] int NOT NULL,
    [ReadyCount] int NOT NULL,
    [ReviewCount] int NOT NULL,
    [ErrorCount] int NOT NULL,
    [ApprovedCount] int NOT NULL,
    [ErrorMessage] nvarchar(max) NULL,
    CONSTRAINT [PK_PriceImportJobs] PRIMARY KEY ([Id])
);

CREATE TABLE [PriceImportMappings] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] int NOT NULL,
    [SupplierName] nvarchar(300) NOT NULL,
    [ExternalColumnName] nvarchar(300) NOT NULL,
    [InternalFieldName] nvarchar(150) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PriceImportMappings] PRIMARY KEY ([Id])
);

CREATE TABLE [PriceLists] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] int NOT NULL,
    [Name] nvarchar(300) NOT NULL,
    [SupplierName] nvarchar(300) NULL,
    [Currency] nvarchar(10) NOT NULL,
    [ValidFrom] datetime2 NULL,
    [ValidTo] datetime2 NULL,
    [SourceFileName] nvarchar(max) NULL,
    [SourceFilePath] nvarchar(max) NULL,
    [SourceFileHash] nvarchar(450) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [ImportedAt] datetime2 NULL,
    [IsActive] bit NOT NULL,
    [Note] nvarchar(max) NULL,
    CONSTRAINT [PK_PriceLists] PRIMARY KEY ([Id])
);

CREATE TABLE [PriceImportCandidates] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] int NOT NULL,
    [ImportJobId] uniqueidentifier NOT NULL,
    [ArticleNumber] nvarchar(150) NULL,
    [ProductCode] nvarchar(150) NULL,
    [Name] nvarchar(500) NOT NULL,
    [Description] nvarchar(max) NULL,
    [CategoryName] nvarchar(max) NULL,
    [BasePrice] decimal(18,4) NULL,
    [DiscountPercent] decimal(9,4) NULL,
    [NetPrice] decimal(18,4) NULL,
    [Unit] nvarchar(50) NULL,
    [Currency] nvarchar(10) NOT NULL,
    [SupplierName] nvarchar(max) NULL,
    [PageNumber] int NULL,
    [SheetName] nvarchar(max) NULL,
    [CellRange] nvarchar(max) NULL,
    [SourceText] nvarchar(max) NULL,
    [RawJson] nvarchar(max) NULL,
    [Confidence] decimal(5,4) NOT NULL,
    [Status] int NOT NULL,
    [ErrorMessage] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_PriceImportCandidates] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PriceImportCandidates_PriceImportJobs_ImportJobId] FOREIGN KEY ([ImportJobId]) REFERENCES [PriceImportJobs] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [PriceListItems] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] int NOT NULL,
    [PriceListId] uniqueidentifier NOT NULL,
    [ArticleNumber] nvarchar(150) NULL,
    [ProductCode] nvarchar(150) NULL,
    [Name] nvarchar(500) NOT NULL,
    [Description] nvarchar(max) NULL,
    [CategoryName] nvarchar(max) NULL,
    [ClassificationPath] nvarchar(max) NULL,
    [BasePrice] decimal(18,4) NULL,
    [DiscountPercent] decimal(9,4) NULL,
    [NetPrice] decimal(18,4) NULL,
    [Unit] nvarchar(50) NULL,
    [Currency] nvarchar(10) NOT NULL,
    [SupplierName] nvarchar(450) NULL,
    [ConsumptionFactor] decimal(18,6) NULL,
    [WastePercent] decimal(9,4) NULL,
    [SourceFileName] nvarchar(max) NULL,
    [SourcePageNumber] int NULL,
    [SourceSheetName] nvarchar(max) NULL,
    [SourceCellRange] nvarchar(max) NULL,
    [SourceText] nvarchar(max) NULL,
    [Confidence] decimal(5,4) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedByUserId] nvarchar(max) NULL,
    [IsActive] bit NOT NULL,
    [UserNote] nvarchar(max) NULL,
    CONSTRAINT [PK_PriceListItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_PriceListItems_PriceLists_PriceListId] FOREIGN KEY ([PriceListId]) REFERENCES [PriceLists] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_PriceImportCandidates_ImportJobId] ON [PriceImportCandidates] ([ImportJobId]);

CREATE INDEX [IX_PriceImportCandidates_TenantId_ArticleNumber] ON [PriceImportCandidates] ([TenantId], [ArticleNumber]);

CREATE INDEX [IX_PriceImportCandidates_TenantId_ImportJobId_Status] ON [PriceImportCandidates] ([TenantId], [ImportJobId], [Status]);

CREATE INDEX [IX_PriceImportCandidates_TenantId_Name] ON [PriceImportCandidates] ([TenantId], [Name]);

CREATE INDEX [IX_PriceImportCandidates_TenantId_ProductCode] ON [PriceImportCandidates] ([TenantId], [ProductCode]);

CREATE INDEX [IX_PriceImportCandidates_TenantId_Status] ON [PriceImportCandidates] ([TenantId], [Status]);

CREATE INDEX [IX_PriceImportJobs_TenantId_SourceFileHash] ON [PriceImportJobs] ([TenantId], [SourceFileHash]);

CREATE INDEX [IX_PriceImportJobs_TenantId_Status] ON [PriceImportJobs] ([TenantId], [Status]);

CREATE INDEX [IX_PriceImportJobs_TenantId_SupplierName] ON [PriceImportJobs] ([TenantId], [SupplierName]);

CREATE INDEX [IX_PriceImportMappings_TenantId_SupplierName_ExternalColumnName] ON [PriceImportMappings] ([TenantId], [SupplierName], [ExternalColumnName]);

CREATE INDEX [IX_PriceListItems_PriceListId] ON [PriceListItems] ([PriceListId]);

CREATE INDEX [IX_PriceListItems_TenantId_IsActive] ON [PriceListItems] ([TenantId], [IsActive]);

CREATE INDEX [IX_PriceListItems_TenantId_Name] ON [PriceListItems] ([TenantId], [Name]);

CREATE INDEX [IX_PriceListItems_TenantId_PriceListId] ON [PriceListItems] ([TenantId], [PriceListId]);

CREATE INDEX [IX_PriceListItems_TenantId_PriceListId_ArticleNumber] ON [PriceListItems] ([TenantId], [PriceListId], [ArticleNumber]);

CREATE INDEX [IX_PriceListItems_TenantId_ProductCode] ON [PriceListItems] ([TenantId], [ProductCode]);

CREATE INDEX [IX_PriceListItems_TenantId_SupplierName] ON [PriceListItems] ([TenantId], [SupplierName]);

CREATE INDEX [IX_PriceLists_TenantId_IsActive] ON [PriceLists] ([TenantId], [IsActive]);

CREATE INDEX [IX_PriceLists_TenantId_SourceFileHash] ON [PriceLists] ([TenantId], [SourceFileHash]);

CREATE INDEX [IX_PriceLists_TenantId_SupplierName] ON [PriceLists] ([TenantId], [SupplierName]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260513233546_AddSmartPriceImportCore', N'10.0.7');

COMMIT;
GO

BEGIN TRANSACTION;
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260514001539_DB260201_4', N'10.0.7');

COMMIT;
GO

