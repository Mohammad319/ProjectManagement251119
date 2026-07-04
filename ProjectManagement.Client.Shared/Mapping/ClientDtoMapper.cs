using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Client.Shared.MVVM.Folder;
using ProjectManagement.Client.Shared.MVVM.Offer;
using ProjectManagement.Client.Shared.Model.Application;
using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Shared.Base.Application;
using ProjectManagement.Shared.DTO.App;
using ProjectManagement.Shared.DTO.Calculation;
using ProjectManagement.Shared.DTO.Calculation.Template;
using ProjectManagement.Shared.DTO.Folder;
using ProjectManagement.Shared.DTO.Offer;
using ProjectManagement.Shared.DTO.Project;
using ProjectManagement.Shared.Constants;
using System;
using System.Linq;

namespace ProjectManagement.Client.Shared.Mapping
{
    public static class ClientDtoMapper
    {
        public static ListCalculationMVVM ToListCalculationMVVM(this ListCalculationDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new ListCalculationMVVM
            {
                Id = dto.Id,
                Order = dto.Order,
                IsPrivate = dto.IsPrivate,
                Name = dto.Name ?? string.Empty,
                Code = dto.Code ?? string.Empty,
                Status = dto.Status ?? string.Empty,
                StatusId = dto.StatusId,
                StatusSortOrder = dto.StatusSortOrder,
                StatusColor = dto.StatusColor ?? string.Empty,
                Responsible = dto.Responsible ?? string.Empty,
                CalculationType = dto.CalculationType,
                BidRole = dto.BidRole,
                CalculationRole = dto.CalculationRole,
                CustomCalculationRoleName = dto.CustomCalculationRoleName ?? string.Empty,
                IsLocked = dto.IsLocked,
                LockedAtUtc = dto.LockedAtUtc,
                LockedByUserId = dto.LockedByUserId,
                ApprovedByUserId = dto.ApprovedByUserId,
                ApprovedByName = dto.ApprovedByName ?? string.Empty,
                ApprovedAtUtc = dto.ApprovedAtUtc,
                SourceCalculationId = dto.SourceCalculationId,
                VersionGroupId = dto.VersionGroupId,
                VersionNumber = dto.VersionNumber,
                CreatedFromCalculationId = dto.CreatedFromCalculationId,
                IsCurrentVersion = dto.IsCurrentVersion,
                StatusAllowsProductionCalculation = dto.StatusAllowsProductionCalculation,
                CountsAsSubmittedBid = dto.CountsAsSubmittedBid,
                CountsAsWonBid = dto.CountsAsWonBid,
                CountsAsLostBid = dto.CountsAsLostBid,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                TenderDeadline = dto.TenderDeadline,
                TenderQA = dto.TenderQA,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                Tax = dto.Tax,
                IsArchived = dto.IsArchived,
                Inspector = dto.Inspector ?? string.Empty,
                ProjectName = dto.ProjectName ?? string.Empty,
                FolderName = dto.FolderName ?? string.Empty,
                AddressText = dto.AddressText ?? string.Empty,
                Priority = dto.Priority,
                TimeMonth = dto.TimeMonth,
                ImportInfo = dto.ImportInfo,
                Access = dto.Access,
            };
        }

        public static CalculationMVVM ToCalculationMVVM(this CalculationPageDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var model = new CalculationMVVM
            {
                Tax = (int)Math.Round(dto.Tax),
                Name = dto.Name ?? string.Empty,
                Code = dto.Code ?? string.Empty,
                Company = dto.Company ?? string.Empty,
                Address = dto.Address ?? string.Empty,
                Customer = dto.Customer ?? string.Empty,
                Supervisor = dto.Supervisor ?? string.Empty,
                Inspector = dto.Inspector ?? string.Empty,
                Compensation = dto.Compensation ?? string.Empty,
                Contract = dto.Contract ?? string.Empty,
                TemplateId = dto.TemplateId,
                TemplateColumnId = dto.TemplateColumnId,
                CalculationType = dto.CalculationType,
                BidRole = dto.BidRole,
                CalculationRole = dto.CalculationRole,
                CustomCalculationRoleName = dto.CustomCalculationRoleName ?? string.Empty,
                IsLocked = dto.IsLocked,
                LockedAtUtc = dto.LockedAtUtc,
                LockedByUserId = dto.LockedByUserId,
                ApprovedByUserId = dto.ApprovedByUserId,
                ApprovedByName = dto.ApprovedByName ?? string.Empty,
                ApprovedAtUtc = dto.ApprovedAtUtc,
                SourceCalculationId = dto.SourceCalculationId,
                VersionGroupId = dto.VersionGroupId,
                VersionNumber = dto.VersionNumber,
                CreatedFromCalculationId = dto.CreatedFromCalculationId,
                IsCurrentVersion = dto.IsCurrentVersion,
                CanEdit = dto.CanEdit,
                Sort = dto.Sort?.Clone() ?? new SortConfig(),
                QuanityList = dto.QuanityList is null ? [] : [.. dto.QuanityList],
                Factors = dto.Factors?.Select(ToFactors).ToList() ?? [],
                AdditionalCostEarnings = (double)dto.AdditionalCostEarnings,
                Tasks = dto.Tasks?.Select(ToTaskListMVVM).ToList() ?? [],
            };

            model.DisplayPresets = DisplayOptionsPresetState.Normalize(dto.DisplayPresets);
            // Preset selection is session-only; first table open should use stored IsActive values.
            model.DisplayPresets.ActivePresetId = null;

            if (dto is CalculationPageOtherDepartmentDTO sharedPage)
            {
                model.Tap1 = sharedPage.Tap1;
                model.Tap2 = sharedPage.Tap2;
                model.Tap3 = sharedPage.Tap3;
                model.Tap4 = sharedPage.Tap4;
                model.Tap5 = sharedPage.Tap5;
                model.Tap6 = sharedPage.Tap6;
            }

            return model;
        }

        public static TaskListMVVM ToTaskListMVVM(this TaskListDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new TaskListMVVM
            {
                Id = dto.Id,
                TaskId = dto.TaskId,
                StatusId = dto.StatusId,
                Status = dto.Status ?? string.Empty,
                StatusColor = dto.StatusColor ?? string.Empty,
                OpportunityId = dto.OpportunityId,
                Opportunity = dto.Opportunity ?? string.Empty,
                Metadata = dto.Metadata,
                ProductionNote = dto.ProductionNote,
                ReviewerComment = dto.ReviewerComment,
                Quantity = dto.Quantity,
                Unit = dto.Unit ?? string.Empty,
                Name = dto.Name ?? string.Empty,
                Order = dto.Order,
                Resources = dto.Resources?.Select(ToResourceListMVVM).ToList() ?? [],
                Tasks = [],
            };
        }

        public static ResourceListMVVM ToResourceListMVVM(this ResourceListDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new ResourceListMVVM
            {
                Id = dto.Id,
                TaskId = dto.TaskId,
                OfferId = dto.OfferId,
                OpportunityId = dto.OpportunityId,
                Opportunity = dto.Opportunity ?? string.Empty,
                AccountId = dto.AccountId,
                Account = dto.Account ?? string.Empty,
                AccountCode = dto.AccountCode ?? string.Empty,
                Status = dto.Status ?? string.Empty,
                StatusColor = dto.StatusColor ?? string.Empty,
                StatusId = dto.StatusId,
                ResourceSortId = dto.ResourceSortId,
                ResourceTypeId = dto.ResourceTypeId,
                ResName = dto.ResName ?? string.Empty,
                Sort = dto.Sort ?? string.Empty,
                Offers = dto.Offers?.Select(ToListOfferMVVM).ToList() ?? [],
                Data = dto.Data,
                ProductionNote = dto.ProductionNote,
                ReviewerComment = dto.ReviewerComment,
                Quantity = dto.Quantity,
                Unit = dto.Unit ?? string.Empty,
                Name = dto.Name ?? string.Empty,
                Order = dto.Order,
                Active = dto.Active,
                ResType = dto.ResType,
            };
        }

        public static ListOfferMVVM ToListOfferMVVM(this ListOfferDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new ListOfferMVVM
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion is null ? Array.Empty<byte>() : [.. dto.RowVersion],
                Organisation = dto.Organisation ?? string.Empty,
                OrganisationId = dto.OrganisationId,
                BaseCost = dto.BaseCost,
                Cost = dto.Cost,
                SubCategory = dto.SubCategory ?? string.Empty,
                Category = dto.Category ?? string.Empty,
                Comment = dto.Comment ?? string.Empty,
                Date = dto.Date,
                UCFirstName = dto.UCFirstName ?? string.Empty,
                UCLastName = dto.UCLastName ?? string.Empty,
                UCDepartment = dto.UCDepartment ?? string.Empty,
                UCStatus = dto.UCStatus ?? string.Empty,
                UCTelefone = dto.UCTelefone ?? string.Empty,
                UCMobile = dto.UCMobile ?? string.Empty,
                Contact = dto.Contact ?? string.Empty,
            };
        }

        public static ListOfferCalcInfoMVVM ToListOfferCalcInfoMVVM(this ListOfferCalcInfo dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new ListOfferCalcInfoMVVM
            {
                Id = dto.Id,
                RowVersion = dto.RowVersion is null ? Array.Empty<byte>() : [.. dto.RowVersion],
                Organisation = dto.Organisation ?? string.Empty,
                OrganisationId = dto.OrganisationId,
                BaseCost = dto.BaseCost,
                Cost = dto.Cost,
                SubCategory = dto.SubCategory ?? string.Empty,
                Category = dto.Category ?? string.Empty,
                Comment = dto.Comment ?? string.Empty,
                Date = dto.Date,
                UCFirstName = dto.UCFirstName ?? string.Empty,
                UCLastName = dto.UCLastName ?? string.Empty,
                UCDepartment = dto.UCDepartment ?? string.Empty,
                UCStatus = dto.UCStatus ?? string.Empty,
                UCTelefone = dto.UCTelefone ?? string.Empty,
                UCMobile = dto.UCMobile ?? string.Empty,
                Contact = dto.Contact ?? string.Empty,
                CalcCode = dto.CalcCode ?? string.Empty,
                CalcName = dto.CalcName ?? string.Empty,
                TaskName = dto.TaskName ?? string.Empty,
                TaskCode = dto.TaskCode ?? string.Empty,
                ResName = dto.ResName ?? string.Empty,
                ResCode = dto.ResCode ?? string.Empty,
            };
        }

        public static TemplateMVVM ToTemplateMVVM(this TemplateModelDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            var defaults = new TemplateData();

            return new TemplateMVVM
            {
                Id = dto.Id,
                DepartmentId = dto.DepartmentId,
                Name = dto.Name ?? string.Empty,
                MathRound = dto.MathRound,
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? defaults.Currency : dto.Currency,
                DateFormat = string.IsNullOrWhiteSpace(dto.DateFormat) ? defaults.DateFormat : dto.DateFormat,
                NetCalc = dto.NetCalc?.Clone() ?? new NetCalc(),
                SummarySheet = dto.SummarySheet?.Clone() ?? new SummarySheet(),
            };
        }

        public static TemplateMVVM ToTemplateMVVM(this TemplateListDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new TemplateMVVM
            {
                Id = dto.Id,
                DepartmentId = dto.DepartmentId,
                Name = dto.Name ?? string.Empty,
            };
        }

        public static TemplateColumnMVVM ToTemplateColumnMVVM(this TemplateColumnModelDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new TemplateColumnMVVM
            {
                Id = dto.Id,
                DepartmentId = dto.DepartmentId,
                Name = dto.Name ?? string.Empty,
                Columns = TemplateDefaults.EnsureNetCalcColumns(dto.Columns)
            };
        }

        public static TemplateColumnMVVM ToTemplateColumnMVVM(this TemplateColumnListDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new TemplateColumnMVVM
            {
                Id = dto.Id,
                DepartmentId = dto.DepartmentId,
                Name = dto.Name ?? string.Empty
            };
        }

        public static FolderMVVM ToFolderMVVM(this ListFolderDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new FolderMVVM
            {
                Id = dto.Id,
                Name = dto.Name ?? string.Empty,
                Order = dto.Order,
                Color = dto.Color ?? "#08bf66",
                IsVisible = dto.IsVisible,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                ProjectCount = dto.ProjectCount,
                DepartmentId = dto.DepartmentId,
                DepartmentName = dto.DepartmentName,
                IsSharedGroup = dto.IsSharedGroup,
                IsReadOnlyGroup = dto.IsSharedGroup,
                Projects = [],
            };
        }

        public static ListProjectMVVM ToListProjectMVVM(this ListProjectDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new ListProjectMVVM
            {
                Id = dto.Id,
                Name = dto.Name ?? string.Empty,
                Order = dto.Order,
                Code = dto.Code ?? string.Empty,
                Status = dto.Status ?? string.Empty,
                Color = dto.Color ?? string.Empty,
                Responsible = dto.Responsible ?? string.Empty,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                TenderDeadline = dto.TenderDeadline,
                TenderQA = dto.TenderQA,
                IsArchived = dto.IsArchived,
                IsShared = dto.IsShared,
                Access = dto.Access,
                DepartmentId = dto.DepartmentId,
                CalculationCount = dto.CalculationCount,
                StatusId = dto.StatusId,
                StatusSortOrder = dto.StatusSortOrder,
                CountsAsSubmittedBid = dto.CountsAsSubmittedBid,
                CountsAsWonBid = dto.CountsAsWonBid,
                CountsAsLostBid = dto.CountsAsLostBid,
                CreatedAt = dto.CreatedAt,
                UpdatedAt = dto.UpdatedAt,
                Developer = dto.Developer ?? string.Empty,
                Organisation = dto.Organisation ?? string.Empty,
                ProcurementName = dto.ProcurementName ?? string.Empty,
                ProcurementNumber = dto.ProcurementNumber ?? string.Empty,
                CustomerReference = dto.CustomerReference ?? string.Empty,
                Contract = dto.Contract ?? string.Empty,
                Type = dto.Type ?? string.Empty,
                Inspector = dto.Inspector ?? string.Empty,
                Designer = dto.Designer ?? string.Empty,
                Supervisor = dto.Supervisor ?? string.Empty,
                AddressText = dto.AddressText ?? string.Empty,
                ProcurementMethods = dto.ProcurementMethods ?? string.Empty,
                Compensation = dto.Compensation ?? string.Empty,
                ProcurementProcedure = dto.ProcurementProcedure ?? string.Empty,
                ClientsManager = dto.ClientsManager ?? string.Empty,
                PublicationDate = dto.PublicationDate,
                DecisionDate = dto.DecisionDate,
                ImportInfo = dto.ImportInfo,
                Calculations = [],
            };
        }

        public static SearchProjectsMVVM ToSearchProjectsMVVM(this SearchProjectDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new SearchProjectsMVVM
            {
                Id = dto.Id,
                Name = dto.Name ?? string.Empty,
                Order = dto.Order,
                Code = dto.Code ?? string.Empty,
                Status = dto.Status ?? string.Empty,
                Color = dto.Color ?? string.Empty,
                Responsible = dto.Responsible ?? string.Empty,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                TenderDeadline = dto.TenderDeadline,
                TenderQA = dto.TenderQA,
                FolderId = dto.FolderId,
            };
        }

        public static OpportunityModel ToOpportunityModel(this OpportunityListDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new OpportunityModel
            {
                Id = dto.Id,
                CalculationId = dto.CalculationId,
                OpportunitiesRisks = dto.OpportunitiesRisks ?? string.Empty,
                OpportunityType = dto.OpportunityType ?? string.Empty,
                Metadata = dto.Metadata,
            };
        }

        public static ApplicationModel ToApplicationModel(this ApplicationDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new ApplicationModel
            {
                Id = dto.Id,
                Name = dto.Name ?? string.Empty,
                IsVisible = dto.IsVisible,
                UserId = dto.UserId,
                LastUpdate = dto.LastUpdate,
                DepartmentId = dto.DepartmentId,
                Data = new ApplicationDataModel
                {
                    Description = dto.Data?.Description ?? string.Empty,
                    TemplateType = dto.Data?.TemplateType ?? SelfInspectionTemplateTypes.Checklist,
                    Purpose = dto.Data?.Purpose ?? string.Empty,
                    IsSystemTemplate = dto.Data?.IsSystemTemplate ?? false,
                    SystemTemplateKey = dto.Data?.SystemTemplateKey ?? string.Empty,
                    CopiedFromSystemTemplateKey = dto.Data?.CopiedFromSystemTemplateKey ?? string.Empty,
                    Row = dto.Data?.Rows?.Select(ToRowModel).ToList() ?? []
                }
            };
        }

        public static ApplicationValuesModel ToApplicationValuesModel(this ApplicationValuesDTO dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            return new ApplicationValuesModel
            {
                Id = dto.Id,
                CalculationId = dto.CalculationId,
                ApplicationId = dto.ApplicationId,
                UserId = dto.UserId,
                Name = dto.Name ?? string.Empty,
                Responsible = dto.Responsible ?? string.Empty,
                LastUpdate = dto.LastUpdate,
                Data = dto.Data?.Clone() ?? new ApplicationValuesData(),
                Application = dto.Application?.ToApplicationModel() ?? new ApplicationModel()
            };
        }

        public static ApplicationValuesDTO ToApplicationValuesDto(this ApplicationValuesModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            return new ApplicationValuesDTO
            {
                Id = model.Id,
                CalculationId = model.CalculationId,
                ApplicationId = model.ApplicationId,
                UserId = model.UserId,
                Name = model.Name ?? string.Empty,
                Responsible = model.Responsible ?? string.Empty,
                LastUpdate = model.LastUpdate,
                Data = model.Data?.Clone() ?? new ApplicationValuesData(),
                Application = model.Application is null ? null : model.Application.ToApplicationDto()
            };
        }

        public static ApplicationDTO ToApplicationDto(this ApplicationModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            return new ApplicationDTO
            {
                Id = model.Id,
                DepartmentId = model.DepartmentId,
                Name = model.Name ?? string.Empty,
                IsVisible = model.IsVisible,
                UserId = model.UserId,
                LastUpdate = model.LastUpdate,
                Data = new ApplicationDataDTO
                {
                    Description = model.Data?.Description ?? string.Empty,
                    TemplateType = model.Data?.TemplateType ?? SelfInspectionTemplateTypes.Checklist,
                    Purpose = model.Data?.Purpose ?? string.Empty,
                    IsSystemTemplate = model.Data?.IsSystemTemplate ?? false,
                    SystemTemplateKey = model.Data?.SystemTemplateKey ?? string.Empty,
                    CopiedFromSystemTemplateKey = model.Data?.CopiedFromSystemTemplateKey ?? string.Empty,
                    Rows = model.Data?.Row?.Select(ToRowDto).ToList() ?? []
                }
            };
        }

        private static RowModel ToRowModel(RowDTO dto)
        {
            return new RowModel
            {
                ID = dto.ID,
                Name = dto.Name ?? string.Empty,
                Description = dto.Description ?? string.Empty,
                Style = dto.Style ?? string.Empty,
                StyleRow = dto.StyleRow ?? string.Empty,
                IsVisible = dto.IsVisible,
                Attributes = dto.Attributes?.Select(ToAttributeModel).ToList() ?? []
            };
        }

        private static RowDTO ToRowDto(RowModel model)
        {
            return new RowDTO
            {
                ID = model.ID,
                Name = model.Name ?? string.Empty,
                Description = model.Description ?? string.Empty,
                Style = model.Style ?? string.Empty,
                StyleRow = model.StyleRow ?? string.Empty,
                IsVisible = model.IsVisible,
                Attributes = model.Attributes?.Select(ToAttributeDto).ToList() ?? []
            };
        }

        private static AttributeModel ToAttributeModel(AttributeDTO dto)
        {
            return new AttributeModel
            {
                ID = dto.ID,
                AttributeType = dto.AttributeType,
                Required = dto.Required,
                Order = dto.Order,
                Validation = dto.Validation ?? string.Empty,
                Style = dto.Style ?? string.Empty,
                Value = dto.Value ?? string.Empty
            };
        }

        private static AttributeDTO ToAttributeDto(AttributeModel model)
        {
            return new AttributeDTO
            {
                ID = model.ID,
                AttributeType = model.AttributeType,
                Required = model.Required,
                Order = model.Order,
                Validation = model.Validation ?? string.Empty,
                Style = model.Style ?? string.Empty,
                Value = model.Value ?? string.Empty
            };
        }

        public static Factors ToFactors(this ProjectManagement.Shared.Base.Calculation.OHFactors dto)
        {
            return new Factors
            {
                ResourceType = dto.ResourceType,
                SortId = dto.SortId,
                ResId = dto.ResId,
                IsLocked = dto.IsLocked,
                Earnings = dto.Earnings,
                Key = dto.Key,
                Unit = dto.Unit ?? string.Empty,
                DivisionKey = dto.DivisionKey,
                Selected = dto.Selected ?? "all",
            };
        }
    }
}
