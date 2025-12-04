using ProjectManagement.Client.Shared.ViewModel;
using ProjectManagement.Shared.Base.Application;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectManagement.Client.Shared.Model.Application
{
    public class AttributeModel : AttributeBase
    {
        public string Value { get; set; }

        [JsonIgnore] public DateTime? Date { get; set; } = DateTime.Now;
        [JsonIgnore] public bool? ValueBool { get; set; }
        public dynamic OBJ;

        private T DeserializeValidation<T>() => string.IsNullOrEmpty(Validation) ? Activator.CreateInstance<T>() : JsonSerializer.Deserialize<T>(Validation);

        [JsonIgnore] public ValidDateVM ValdDateVM => DeserializeValidation<ValidDateVM>();
        [JsonIgnore] public ValidSelectVM ValdSelectVM => DeserializeValidation<ValidSelectVM>();
        [JsonIgnore] public ValidTextVM ValdTextVM => DeserializeValidation<ValidTextVM>();
        [JsonIgnore] public ValidNumberVM ValdNumberVM => DeserializeValidation<ValidNumberVM>();
        [JsonIgnore] public ValidBoolVM ValdBoolVM => DeserializeValidation<ValidBoolVM>();
    }

    public class RowModel : RowBase
    {
        public List<AttributeModel> Attributes { get; set; } = [];
    }

    public class ApplicationDataModel : ApplicationDataBase
    {
        public List<RowModel> Row { get; set; } = [];
    }

    public class ApplicationModel : ApplicationBase
    {
        public int Id { get; set; }
        public ApplicationDataModel Data { get; set; } = new();
        public int DepartmentId { get; set; }
    }
}
