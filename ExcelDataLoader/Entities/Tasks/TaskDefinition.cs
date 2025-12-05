using TaskResourceBlueprints.Entities.Lookups;
using TaskResourceBlueprints.Entities.Questions.Assignments;
using TaskResourceBlueprints.Entities.Questions.Conditions;
using TaskResourceBlueprints.Entities.Questions.Groups;
using ProjectManagement.Shared.Helper.ProjectAppStorage;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskResourceBlueprints.Entities.Tasks
{
    public class TaskLookupBase
    {
        public int Id { get; set; }

        /// <summary>
        /// اسم المهمة للعرض.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// ترتيب المهمة في القالب/المشروع.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// هل المهمة ظاهرة في واجهة المستخدم.
        /// </summary>
        public bool IsVisible { get; set; } = true;
    }

    public enum TaskStatusEnum
    {
        ToPlan = 1,
        ToDo = 2,
        UnderWorking = 3,
        Ready = 10,
    }
    public class TaskDefinition : TaskLookupBase
    {
        public TaskStatusEnum Status { get; set; }

        /// <summary>الشخص المسؤول عن المهمة.</summary>
        public string? Responsible { get; set; }

        /// <summary>ملاحظات إدارية داخلية.</summary>
        public string? AdminNote { get; set; }

        /// <summary>ملاحظات عامة عن المهمة.</summary>
        public string? FieldNotes { get; set; }  // أو Notes / Description حسب ما تفضّل

        public double? Quantity { get; set; }
        public string? UnitCode { get; set; }

        public double ChangeFactor1 { get; set; } = 1;
        public double ChangeFactor2 { get; set; } = 1;

        public bool IsActive { get; set; } = true;
        public bool Uncontrollable { get; set; }

        /// <summary>Code داخلي للمهمة (إن وجد).</summary>
        public string? Code { get; set; }

        /// <summary>
        /// Workload thresholds, e.g. for Low / Medium / High.
        /// Always three values.
        /// </summary>
        public List<double> WorkloadThresholds { get; set; } = new() { 0, 0, 0 };

        // Lookups
        public int? ActionId { get; set; }
        public int? LocationId { get; set; }
        public int? ActionTypeId { get; set; }
        public int? FallId { get; set; }
        public int? TaskUnitGroupId { get; set; }

        /// <summary>ملاحظات متعلقة بعرض المهمة كسطر (Row) في الواجهة.</summary>
        public List<string> RowNotes { get; set; } = [];

        /// <summary>المجلدات الظاهرة لهذه المهمة.</summary>
        public List<int> VisibleFolderIds { get; set; } = [];

        public int? CapacityResourceId { get; set; }

        [NotMapped]
        public string? NewUnitCode { get; set; }

        public ActionEntity? Action { get; set; }
        public LocationEntity? Location { get; set; }
        public ActionTypeEntity? ActionType { get; set; }
        public FallEntity? Fall { get; set; }
        public TaskUnitGroup? TaskUnitGroup { get; set; }

        public List<TaskResourceAssignment> TaskResourceAssignments { get; set; } = [];
        public List<QuestionGroupDefinition> QuestionGroups { get; set; } = [];
        public List<ResourceSelectorDefinition> ResourceSelectors { get; set; } = [];
        public List<NumericQuestionDefinition> NumericQuestions { get; set; } = [];
        public List<ConditionDefinition> Conditions { get; set; } = [];
    }
}
