using ProjectManagement.Client.Helper.Converter;
using System;
using System.Collections.Generic;

namespace ProjectManagement.Client.Helper.DropDown
{
    public static class DropDownHelper
    {
        static string Order = "Order";
        public static double HandleDrop<T>(T folder, T CalcDraging, List<T> list)
        {
            if (CalcDraging is not null)
            {
                int index = list.IndexOf(folder);
                if (index == 0)
                    SetOrder(CalcDraging, GetOrder(folder) + 100);
                else if (GetOrder(CalcDraging) < GetOrder(folder))
                    SetOrder(CalcDraging, (GetOrder(folder) + GetOrder(list[index - 1])) / 2);
                else if (GetOrder(CalcDraging) > GetOrder(folder) && (index + 1) < list.Count)
                    SetOrder(CalcDraging, (GetOrder(folder) + GetOrder(list[index + 1])) / 2);
                else SetOrder(CalcDraging, GetOrder(folder) - 100);
                return GetOrder(CalcDraging);
            }
            return -1;
        }

        static double GetOrder<TItem>(TItem item) => Convert.ToDouble(ConverterHelper.Get(item, Order));
        static void SetOrder<TItem>(TItem item, double order) => ConverterHelper.SetValue(item, Order, order);
    }
}
