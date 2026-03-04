using ClosedXML.Excel;
using Microsoft.AspNetCore.Components.Forms;
using ProjectManagement.Shared.Base.Calculation;
using ProjectManagement.Shared.DTO.Calculation;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;
namespace ProjectManagement.Client.Helper
{
    public class ExcelHelper
    {
        /// <summary>
        /// استيراد مهام من ملف Excel وتحويلها إلى قائمة من TaskPostDTO.
        /// </summary>
        public static List<TaskPostDTO> Import(int sheetNr, XLWorkbook workbook, int rowStart,
            int codeIndex, int nameIndex, int unitIndex, int quantityIndex, int priceIndex, bool isOH)
        {
            int belopIndex = codeIndex + 7;
            List<TaskPostDTO> sections = new();

            IXLWorksheet sheet = workbook.Worksheet(sheetNr);

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            for (int row = rowStart; row <= lastRow; row++)
            {
                if (sheet.Row(row).IsEmpty()) continue;

                sheet.Cell(row, codeIndex).TryGetValue(out string code);

                if (sheet.Cell(row, nameIndex).TryGetValue(out string name))
                {
                    sheet.Cell(row, unitIndex).TryGetValue(out string unit);
                    sheet.Cell(row, quantityIndex).TryGetValue(out string quantityStr);
                    sheet.Cell(row, priceIndex).TryGetValue(out string priceStr);
                    sheet.Cell(row, belopIndex).TryGetValue(out string belopStr);

                    var task = new TaskPostDTO
                    {
                        Name = !string.IsNullOrWhiteSpace(name) ? name : "Task",
                        Tasks = new(),
                        Colspan = false,
                        Metadata = new TaskMetadata
                        {
                            Code = code,
                            Unit = unit,
                            Type = TaskType.Task,
                            IsOH = isOH
                        }
                    };

                    if (string.IsNullOrWhiteSpace(unit) && string.IsNullOrWhiteSpace(priceStr) && string.IsNullOrWhiteSpace(quantityStr))
                    {
                        task.Metadata.Type = TaskType.CodeName;
                        task.Metadata.Quantity = null;
                    }
                    else if (unit == "-" && quantityStr == "-" && priceStr == "-")
                    {
                        task.Metadata.Type = TaskType.Minus;
                        task.Metadata.Quantity = belopStr == "-" ? 0 : 1;
                    }
                    else
                    {
                        task.Metadata.QuantityParam = ConstValues.FixedQ;
                        _ = decimal.TryParse(quantityStr, out decimal quantity);
                        _ = decimal.TryParse(priceStr, out decimal price);
                        task.Metadata.Quantity = quantity;
                        task.Metadata.PriceSubDB = price;
                    }

                    if (!string.IsNullOrWhiteSpace(task.Name))
                        sections.Add(task);
                }
            }

            ReSort(sections);
            return sections;
        }

        /// <summary>
        /// إعادة تنظيم المهام حسب تسلسل الكود الأب والابن.
        /// </summary>
        static void ReSort(List<TaskPostDTO> sections)
        {
            for (int child = sections.Count - 1; child >= 0; child--)
            {
                for (int parent = child - 1; parent >= 0; parent--)
                {
                    bool parentHasCode = !string.IsNullOrEmpty(sections[parent].Metadata.Code);
                    bool childHasCode = !string.IsNullOrEmpty(sections[child].Metadata.Code);

                    if ((parentHasCode && childHasCode && sections[child].Metadata.Code.StartsWith(sections[parent].Metadata.Code))
                        || (!childHasCode && parentHasCode))
                    {
                        sections[parent].Tasks.Insert(0, sections[child]);
                        sections.RemoveAt(child);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// تصدير DataTable إلى ملف Excel في المسار المحدد.
        /// </summary>
        public static void Export(DataTable data, string sheetName, string path)
        {
            using XLWorkbook workbook = new();
            workbook.AddWorksheet(data, sheetName);
            using MemoryStream memory = new();
            workbook.SaveAs(memory);
            File.WriteAllBytes(path, memory.ToArray());
        }
    }

}
