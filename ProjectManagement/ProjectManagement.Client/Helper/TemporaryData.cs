using ProjectManagement.Shared.DTO.Project;

namespace ProjectManagement.Client.Helper
{
    public static class SelectedData
    {
        public static CalculationItemType? ItemsType { get; set; }
        public static List<ResourceTaskItemDTO> SelectedItems { get; private set; } = [];
        public static bool ExistItem(CalculationItemType type, int id) =>
            ItemsType == type && SelectedItems.Any(x => x.Id == id);

        public static void Add(int id, double? q, CalculationItemType type)
        {
            if (type != ItemsType)
            {
                SelectedItems.Clear();
                ItemsType = type;
            }
            if (SelectedItems.Any(x => x.Id == id))
            {
                var o = SelectedItems.FirstOrDefault(x => x.Id == id);
                SelectedItems.Remove(o);
            }
            else SelectedItems.Add(new ResourceTaskItemDTO(id, q));
        }

        public static void Reset()
        {
            ItemsType = null;
            SelectedItems.Clear();
        }
    }
    public static class TemporaryData
    {
        public static void Copy(int calcid, int id, double? q, CalculationItemType type)
            => CopyCut(calcid, id, q, type, CopyType.Copy);
        public static void Cut(int calcid, int id, double? q, CalculationItemType type)
    => CopyCut(calcid, id, q, type, CopyType.Move);
        static void CopyCut(int calcid, int id, double? q, CalculationItemType type, CopyType ctype)
        {
            TemporaryData.Reset();
            if (!SelectedData.ExistItem(type, id)) TemporaryData.SelectedItems.Add(new ResourceTaskItemDTO(id, q));
            else
            {
                TemporaryData.SelectedItems.AddRange(SelectedData.SelectedItems);
            }
            SelectedData.Reset();
            TemporaryData.CopyTypeo = ctype;
            TemporaryData.ItemsType = type;
            TemporaryData.OldCalcID = calcid;
        }
        public static string Key { get; set; }
        public static List<ResourceTaskItemDTO> SelectedItems { get; set; } = [];
        public static int OldCalcID { get; set; }
        public static CalculationItemType? ItemsType { get; set; }
        public static CopyType? CopyTypeo { get; set; }
        public static bool HasPasteOption(CalculationItemType t)
        {
            if (SelectedItems.Any() && ItemsType == t && (CopyTypeo == CopyType.Copy
                || CopyTypeo == CopyType.Move))
                return true;
            return false;
        }

        public static void Reset()
        {
            ItemsType = null;
            CopyTypeo = null;
            SelectedItems.Clear();
        }
    }
}
