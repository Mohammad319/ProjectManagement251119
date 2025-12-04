using ProjectManagement.Shared.Base.Project;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagement.Shared.Base.ProjectAppStorage
{
    public class TaskConditionBase
    {
        public int Id { get; set; }
        public ConditionLogic OptionToResourceLogic { get; set; } = ConditionLogic.And;
        public ConditionLogic OptionToNumericLogic { get; set; } = ConditionLogic.And;
        public ConditionLogic NumericToResourceLogic { get; set; } = ConditionLogic.And;
    }
}
