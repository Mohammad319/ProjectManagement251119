using Domain.Entities.Base;
using Domain.Entities.Calculation;
using Domain.Entities.Users;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Constant;
using ProjectManagement.Shared.DTO.ResourceType;
using ProjectManagement.Shared.Enums;
using ProjectManagement.Shared.Resource;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Domain.Entities.ResourceType
{
    public class ResourceTypeEntity : IDataKeyFilterReadOnly
    {
        public int Id { get; set; }
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ResLocalize))]
        [MaxLength(80, ErrorMessageResourceName = ErrorsMessages.MaxLength, ErrorMessageResourceType = typeof(ResLocalize))]
        public string Name { get; set; }
        public int Order { get; set; }
        public bool IsVisible { get; set; } = true;
        [Required(ErrorMessageResourceName = ErrorsMessages.FieldIsRequred, ErrorMessageResourceType = typeof(ResLocalize))]
        public ResourceTypesEnum Type { get; set; }
        [JsonIgnore] public ICollection<ResourceSortEntity> ResourcesSort { get; set; }
        [JsonIgnore] public AccountEntity Account { get; set; }
        public int? AccountId { get; set; }
        [JsonIgnore] public int TenantId { get; set; }
        ResourceTypeData data;
        public ResourceTypeData Data
        {
            get
            {
                data ??= new ResourceTypeData();
                return data;
            }
            set
            {
                data = value;
            }
        }
        [JsonIgnore] public ICollection<ResourceEntity> Resources { get; set; } 

        //public int PMCustomerId { get; set; }
        //public PMCustomerEntity PMCustomer { get; set; }
    }

    public class ResourceSortEntity : ResourceSortForm, IDataKeyFilterReadOnly
    {
        ResourceTypeData data;
        public ResourceTypeData Data
        {
            get
            {
                data ??= new ResourceTypeData();
                return data;
            }
            set
            {
                data = value;
            }
        }
        [Key] public int Id { get; set; }

        [JsonIgnore]public ResourceTypeEntity ResourceType { get; set; }
        public int ResourceTypeId { get; set; }
        public int? AccountId { get; set; }
        public AccountEntity Account { get; set; }
        [JsonIgnore] public ICollection<ResourceEntity> Resources { get; set; }

        [JsonIgnore] public int TenantId { get; set; }
    }
}
