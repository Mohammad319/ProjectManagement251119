using ProjectManagement.Client.Shared.Constants;
using ProjectManagement.Shared.DTO.Calculation.Template;
using System.Text;

namespace ProjectManagement.Client.Shared.MVVM.Calculation
{
    public class TemplateMVVM : TemplateData
    {
        public double Format(double x) => UiStyles.Format(x, MathRound);
        public double Format(decimal x) => UiStyles.Format((double)x, MathRound);
        public double Format(decimal? x) => UiStyles.Format((double)(x ?? 0m), MathRound);
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
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

        public int StartCol1 { get; set; }

        public string FreezCol()
        {
            StyleNetCalc = string.Empty;
            int startCol = StartCol1;
            var sb = new StringBuilder();
            sb.Append(SetFreezCol(1, 0));
            for (int i = 0; i < NetCalc.Columns.Count; i++)
            {
                if (NetCalc.Columns[i].Frozen)
                {
                    NetCalc.Columns[i].StartPX = startCol;
                    startCol = startCol + NetCalc.Columns[i].Width;
                    sb.Append(SetFreezCol(i + 2, NetCalc.Columns[i].StartPX));
                }
                sb.Append($" .colH{i + 2}" + "{" +
"white-space: nowrap; overflow: hidden; text-overflow: ellipsis;}");

            }
            //        foreach (var item in NetColumnsToUse)
            //        {
            //            if (item.Frozen)
            //            {
            //                item.StartPX = startCol;
            //                startCol = startCol + item.Width;
            //                sb.Append(SetFreezCol(item.Id + 2, item.StartPX));
            //            }
            //            sb.Append($" .colH{item.Id + 2}" + "{" +
            //"white-space: nowrap; overflow: hidden; text-overflow: ellipsis;}");
            //        }
            StyleNetCalc = sb.ToString();
            return StyleNetCalc;
        }
    }
}
