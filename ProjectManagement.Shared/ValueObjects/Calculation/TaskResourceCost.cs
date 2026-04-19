
using System;

namespace ProjectManagement.Shared.ValueObjects.Calculation
{
    public sealed class CostValue
    {
        /// <summary>
        /// التكلفة النهائية كما تُعرض في الواجهة أو تُخزّن.
        /// </summary>
        public decimal Cost { get; private set; }

        /// <summary>
        /// التكلفة الأساسية قبل التعديلات (اختياري).
        /// </summary>
        public decimal? BaseCost { get; private set; }

        /// <summary>
        /// عوامل التعديل (اختيارية).
        /// </summary>
        public decimal? ChangeFactor1 { get; private set; }
        public decimal? ChangeFactor2 { get; private set; }

        private CostValue() { } // للـ EF

        public CostValue(
            decimal cost,
            decimal? baseCost = null,
            decimal? changeFactor1 = null,
            decimal? changeFactor2 = null)
        {
            Set(cost, baseCost, changeFactor1, changeFactor2);
        }

        private void Set(
            decimal cost,
            decimal? baseCost,
            decimal? changeFactor1,
            decimal? changeFactor2)
        {
            if (cost < 0)
                throw new ArgumentOutOfRangeException(nameof(cost), "Cost cannot be negative.");

            Cost = cost;
            BaseCost = baseCost;
            ChangeFactor1 = changeFactor1;
            ChangeFactor2 = changeFactor2;
        }
    }
}
