using System;

namespace ProjectManagement.Shared.ValueObjects.Calculation
{
    public sealed class DateRange
    {
        public DateTime Start { get; private set; }
        public DateTime End { get; private set; }

        // EF Core constructor
        private DateRange() { }

        public DateRange(DateTime start, DateTime end)
        {
            if (end < start)
                throw new ArgumentException("End date cannot be earlier than start date");

            Start = start;
            End = end;
        }

        public bool Contains(DateTime date)
            => date >= Start && date <= End;

        public override string ToString()
            => $"{Start:yyyy-MM-dd} → {End:yyyy-MM-dd}";
    }
}
