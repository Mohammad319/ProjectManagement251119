using ProjectManagement.Client.Shared.Model.Project.Calculation;
using ProjectManagement.Client.Shared.ViewModel;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    public interface ICalcRow
    {
        string ResName { get; }
        double? Quantity { get; }
        string QuantityParam { get; }
        string Unit { get; }
        double ChangeFactor1 { get; }
        double ChangeFactor2 { get; }
        double? BaseCost { get; }
        double NetCostQ { get; }
        double NetCostTotaly { get; }
        double ApriceTotally { get; }
        double? TotalCO2 { get; }
        string Status { get; }
        string StatusColor { get; }
        int? StatusId { get; }
        bool FilterVisible { get; set; }
        bool HasUpdated { get; set; }
        bool IsDragOver { get; set; }
    }

    public class ListCalculationMVVM : ListCalculationDTO
    {
        public bool IsDragOver { get; set; }
    }


    public class FlatItem
    {
        public int Index { get; set; } // سنستخدمه لرقم السطر

        public TaskListMVVM Task { get; set; }
        public ResourceListMVVM Resource { get; set; }

        public bool IsTask => Task != null;
        public bool IsResource => Resource != null;

        public int Depth { get; set; }  // للتحكم في المسافة البادئة Left

        // تحسين: توليد الـ Key مرة واحدة فقط، وعدم إنشاء Guid في كل قراءة
        private string _versionKey;
        public string VersionKey
        {
            get
            {
                if (_versionKey != null)
                    return _versionKey;

                if (Task is not null)
                    _versionKey = $"T_{Task.Id}";
                else if (Resource is not null)
                    _versionKey = $"R_{Resource.Id}";
                else
                    _versionKey = Guid.NewGuid().ToString();

                return _versionKey;
            }
        }
    }


    public class CalculationMVVM
    {
        public void BuildTaskHierarchy()
        {
            var lookup = Tasks.Where(x => x.TaskId != null).GroupBy(x => x.TaskId).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var task in Tasks)
            {
                if (lookup.TryGetValue(task.Id, out var children)) task.Tasks = children;
                else task.Tasks = [];
            }
        }
        public List<FlatItem> AllFlatItems { get; set; }
        public List<FlatItem> BuildFlatList(IEnumerable<TaskListMVVM> tasks)
        {
            var flat = new List<FlatItem>();
            int index = 0;
            BuildFlatListInternal(tasks, depth: 0, flat, ref index);
            return flat;
        }

        private void BuildFlatListInternal(
            IEnumerable<TaskListMVVM> tasks,
            int depth,
            List<FlatItem> flat,
            ref int index)
        {
            foreach (var task in tasks.Where(x =>
                         x.FilterVisible &&
                         x.IsOH == OHFactors &&
                         (!OnlyActive || x.Active)))
            {
                // صف المهمة نفسها
                flat.Add(new FlatItem
                {
                    Task = task,
                    Depth = depth,
                    Index = index++
                });

                // لو المهمة ليست مفتوحة (CollSpan=false) لا نضيف أولادها
                if (!task.CollSpan)
                    continue;

                // الموارد التابعة للمهمة
                if (task.Resources != null)
                {
                    foreach (var r in task.Resources.Where(r => r.FilterVisible))
                    {
                        flat.Add(new FlatItem
                        {
                            Resource = r,
                            Depth = depth + 1,
                            Index = index++
                        });
                    }
                }

                // المهام الفرعية
                if (task.Tasks != null && task.Tasks.Count > 0)
                {
                    BuildFlatListInternal(task.Tasks, depth + 1, flat, ref index);
                }
            }
        }

        public int Id { get; set; }
        public double Tax { get; set; }

        public string Name { get; set; }
        public string Code { get; set; }
        public string Company { get; set; }
        public string Address { get; set; }
        public string Customer { get; set; }
        public string Supervisor { get; set; }
        public string Inspector { get; set; }
        public string Compensation { get; set; }
        public string Contract { get; set; }

        public int? TemplateId { get; set; }

        public bool Tap1 { get; set; } = true;
        public bool Tap2 { get; set; } = true;
        public bool Tap3 { get; set; } = true;
        public bool Tap4 { get; set; } = true;
        public bool Tap5 { get; set; } = true;
        public bool Tap6 { get; set; } = true;

        public List<QuanityListDTO> QuanityList { get; set; }
        public virtual List<TaskListMVVM> Tasks { get; set; }
        public List<Factors> Factors { get; set; } = [];

        public double AdditionalCostEarnings { get; set; } = 10;
        public double ResourcePriceSum { get; set; } = 0;

        public double Sum => Factors.Sum(x => x.Sum);
        public double SumFactorsEV => Factors.Sum(x => x.EarningsValue);
        public double SumFactorsPrice => Factors.Sum(x => x.Price);

        public double ProfitDecision { get; set; }
        public double TenderExcelTax { get; set; }
        public double TenderInclTax { get; set; }


        public bool OnlyActive { get; set; }
        public bool ShowTasks { get; set; } = true;
        public bool ShowResources { get; set; } = true;
        public bool ShowComment { get; set; } = true;

        public bool OHFactors { get; set; }
        public List<HourlyPriceListGroupDTO> HourlyPriceList { get; set; } = [];
        public List<OpportunityModel> Opportunities { get; set; }

        public TemplateMVVM Template { get; set; } = new();
        public FilterVM FilterVM { get; set; }
        public event Action OnChangeInCalculation;
        public void RefreshCalculation()
        {
            OnChangeInCalculation?.Invoke();
        }
    }

}

