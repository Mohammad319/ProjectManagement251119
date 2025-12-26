using ProjectManagement.Client.Constant;
using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    public class TemplateMVVM : TemplateData
    {
        public string Format(object x) => UiStyles.Format(x, MathRound);
        public double Format(double x) => UiStyles.Format(x, MathRound);
        public int Id { get; set; }
        public string Name { get; set; }

        public void ReOrder(int index, int target)
        {
            int s = NetOrder[index];
            NetOrder[index] = NetOrder[target];
            NetOrder[target] = s;
        }

        static string SetFreezCol(int ColNum, int w)
        {
            string className = "divNetCalc";
            string style = "";
            style += $".{className} th:nth-child({ColNum}),.{className} td:nth-child({ColNum})" + "{" +
                     $"position:sticky;left:{w}px;" + "}";
            style += $".{className} th:nth-child({ColNum})" + "{z-index:15;}";
            style += $".{className} td:nth-child({ColNum})" + "{background:inherit;}";
            return style;
        }

        public string StyleNetCalc { get; set; } = string.Empty;

        public int NoteColspanLeft = 0;
        public int NoteColspanLeftPX = 0;
        public int StartCol1 = 35; //50

        public string FreezCol()
        {
            int w = StartCol1;

            // مهم: إعادة تصفير القيم لتفادي التراكم مع كل استدعاء
            StyleNetCalc = string.Empty;
            NoteColspanLeft = 0;
            NoteColspanLeftPX = 0;

            var sb = new StringBuilder();

            foreach (var item in NetOrder)
            {
                NoteColspanLeft++;
                NoteColspanLeftPX += NetWidth[item];

                if (CalcConst.NetCall[item].TN == nameof(TaskListMVVM.Name))
                    break;
            }

            sb.Append(SetFreezCol(1, 0)); // أول عمود

            List<int> ColmunsNum = new();
            foreach (var freezindex in FreezList)
                ColmunsNum.Add(NetOrder.IndexOf(freezindex));

            ColmunsNum = ColmunsNum.Where(x => x > -1).OrderBy(x => x).ToList();

            foreach (var indexOfNetOrder in ColmunsNum)
            {
                int ColNum = indexOfNetOrder + 2;
                sb.Append(SetFreezCol(ColNum, w));
                w += NetWidth[NetOrder[indexOfNetOrder]];
            }

            for (int i = 0; i < NetOrder.Count; i++)
            {
                sb.Append($" .colH{i + 2}" + "{" +
                          "white-space: nowrap; overflow: hidden; text-overflow: ellipsis;}");
            }

            StyleNetCalc = sb.ToString();
            return StyleNetCalc;
        }
    }
}