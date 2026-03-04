using ProjectManagement.Client.Helper;
using ProjectManagement.Client.Shared.MVVM.Calculation;
using ProjectManagement.Shared.Base.Calculation;

namespace ProjectManagement.Client.Extensions.CalcultationItemsOperation
{
    public static class ResourceExtensions
    {
        public static string Style(this ResourceListMVVM resource, string color, bool isTaskActive)
        {
            if (SelectedData.ExistItem(CalculationItemType.resource, resource.Id))
                return CSS.SelectedItem;
            return isTaskActive && resource.Active
                ? $"background-color:{color}"
                : $"background-color:{color};color:rgb(49 48 45 / 55%)";
        }
        public static bool Add(this CalculationMVVM calculation, ResourceListMVVM resource)
            => calculation.Add([resource]);

        public static bool Add(this CalculationMVVM calculation, List<ResourceListMVVM> list)
        {
            if (list == null || list.Count == 0) return false;
            var task = calculation.Tasks.FirstOrDefault(x => x.Id == list.First().TaskId);
            if (task == null) return false;
            task.Resources.InsertRange(0, list);
            return true;
        }
        public static bool RemoveResources(this CalculationMVVM calculation, IEnumerable<int> resourceIds)
        {
            if (resourceIds == null) return false;

            foreach (int id in resourceIds)
            {
                var task = calculation.Tasks.FirstOrDefault(x => x.Resources.Any(r => r.Id == id));
                var resourceToRemove = task?.Resources?.FirstOrDefault(r => r.Id == id);
                if (resourceToRemove != null)
                    task.Resources.Remove(resourceToRemove);
            }

            return true;
        }

        public static void CalcVaribles(
            this ResourceListMVVM resource,
            Dictionary<string, QuanityListDTO> qIndex,
            decimal? taskQuantity,
            decimal? cap)
        {
            if (resource.Data is null)
                throw new InvalidOperationException("resource.Data must not be null.");

            var data = resource.Data;

            if (resource.HasCap && cap.HasValue)
                data.CapWaste = cap.Value;

            var effectiveTaskQuantity = taskQuantity ?? 0m;

            if (!string.IsNullOrEmpty(resource.QuantityParam))
            {
                if (qIndex.TryGetValue(resource.QuantityParam, out var matched))
                {
                    data.Quantity = matched.Quantity;
                }
                else
                {
                    data.QuantityParam = ConstValues.FixedQ;
                }
            }
            else
            {
                var baseCalc = effectiveTaskQuantity * resource.ChangeFactor1 * resource.ChangeFactor2;
                var capWaste = data.CapWaste;

                if (resource.HasWast && capWaste != 0)
                    data.Quantity = baseCalc * (1 + capWaste / 100);
                else if (resource.HasCap && capWaste != 0)
                    data.Quantity = baseCalc / capWaste;
                else
                    data.Quantity = baseCalc;
            }
        }
    }
}