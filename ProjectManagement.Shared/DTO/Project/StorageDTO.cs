using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ProjectManagement.Shared.DTO.Project
{
    public sealed record ResourceTaskItemDTO(int Id, double? Value);

    public class PostStorygeDTO
    {
        public int OldCalcID { get; set; }
        public int NewCalcID { get; set; }
        public int ParentID { get; set; }
        public CalculationItemType Type { get; set; } = default!;
        public bool WithCildren { get; set; }
        public bool IsOH { get; set; }
        public List<ResourceTaskItemDTO> Items { get; set; } = [];
        public CopyType copyType { get; set; } = default!;
    }
    public class StorageDTO<T>
    {
        public int Id { get; set; }
        public T StorageValue { get; set; } = default!;
        public CalculationItemType StorageType { get; set; } = default!;
        public StorageSort StorageSort { get; set; } = default!;
        public AuthorityStorage StorageLevel { get; set; } = default!;
    }
    public class GetFilterDTO
    {
        public AuthorityStorage AuthoritySelected { get; set; } = AuthorityStorage.program;
        public StorageSort SortSelected { get; set; } = StorageSort.Construction;
        public CalculationItemType Type { get; set; } = CalculationItemType.task;
        public string ItemCalcCategory { get; set; } = string.Empty;
    }
}
