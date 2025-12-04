using Domain.Entities.Application;
using Domain.Entities.Base;
using Domain.Entities.Folder;
using Domain.Entities.Organisation;
using Domain.Entities.Project;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Account;
using ProjectManagement.Shared.DTO.Calculation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Domain.Entities.Calculation
{
    public class CalculationEntity : CalculationBase, IDataKeyFilterReadOnly
    {
        public CalculationEntity()
        {
            Tasks = [];
        }
        [Key] public int Id { get; set; }
        CalculationData data = new();
        public CalculationData Data { get { data ??= new CalculationData(); return data; } set { data = value; } }
        CalculationHourlyPriceFactorData hourlyPriceFactor = new();
        public CalculationHourlyPriceFactorData HourlyPriceFactorData { get { hourlyPriceFactor ??= new CalculationHourlyPriceFactorData(); return hourlyPriceFactor; } set { hourlyPriceFactor = value; } }

        [JsonIgnore] public int TenantId { get; set; }
        public bool IsPrivate { get; set; }
        public bool IsVisible { get; set; } = true;
        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime? LastModified { get; set; }
        public int? OrganisationId { get; set; }
        [ForeignKey(nameof(OrganisationId))] public OrganisationEntity Organisation { get; set; }
        public int? TypeId { get; set; }
        [JsonIgnore][ForeignKey(nameof(TypeId))] public TypeEntity Type { get; set; }
        public int? StatusId { get; set; }
        [ForeignKey(nameof(StatusId))] public StatusEntity Status { get; set; }
        public int? ProcurementMethodsId { get; set; }
        [JsonIgnore][ForeignKey(nameof(ProcurementMethodsId))] public ProcurementMethodsEntity ProcurementMethods { get; set; }
        public int? CompensationId { get; set; }
        [JsonIgnore][ForeignKey(nameof(CompensationId))] public CompensationEntity Compensation { get; set; }
        public int? ContractId { get; set; }
        [JsonIgnore][ForeignKey(nameof(ContractId))] public ContractEntity Contract { get; set; }
        public Guid ProjectId { get; set; }
        [JsonIgnore][ForeignKey(nameof(ProjectId))] public ProjectEntity Project { get; set; }
        public int? UserId { get; set; }
        [ForeignKey(nameof(UserId))][JsonIgnore] public UserEntity User { get; set; }
        public int? TemplateId { get; set; }
        [JsonIgnore] public TemplateEntity Template { get; set; }
        [JsonIgnore] public ICollection<AttributeNameTenderEntity> AttributesTender { get; set; }
        [JsonIgnore] public ICollection<TenderEntity> Tenders { get; set; }
        public List<TaskEntity> Tasks { get; set; }
        public ICollection<ShareCalcEntity> SharesCalc { get; set; }
        [JsonIgnore] public ICollection<OfferEntity> Offers { get; set; }
        [JsonIgnore] public ICollection<OpportunityEntity> Opportunities { get; set; }
        [JsonIgnore] public ICollection<ApplicationValuesEntity> Applications { get; set; }

    }
}
