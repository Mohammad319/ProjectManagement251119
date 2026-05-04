using System;
using System.Collections.Generic;

namespace ProjectManagement.Shared.Helper.ProjectAppStorage
{
    file static class TupleComparer
    {
        public static IEqualityComparer<(T1, T2)> Create<T1, T2>(
            IEqualityComparer<T1> c1, IEqualityComparer<T2> c2) =>
            new Impl<T1, T2>(c1, c2);

        private sealed class Impl<T1, T2>(IEqualityComparer<T1> c1, IEqualityComparer<T2> c2)
            : IEqualityComparer<(T1, T2)>
        {
            public bool Equals((T1, T2) x, (T1, T2) y)
                => c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2);
            public int GetHashCode((T1, T2) obj)
                => System.HashCode.Combine(c1.GetHashCode(obj.Item1!), c2.GetHashCode(obj.Item2!));
        }
    }

    public static class UnitRulesCatalog
    {
        public static readonly Dictionary<(string From, string To), UnitRule> Rules = CreateRules();

        private static Dictionary<(string From, string To), UnitRule> CreateRules()
        {
            var ic = StringComparer.OrdinalIgnoreCase;
            var rules = new Dictionary<(string, string), UnitRule>(
                TupleComparer.Create<string, string>(ic, ic));

            void Add(UnitRule unitRule)
            {
                rules[(unitRule.FromUnit, unitRule.ToUnit)] = unitRule;

                var reverse = unitRule.Reverse();
                if (reverse is null)
                    return;

                var reverseKey = (reverse.FromUnit, reverse.ToUnit);
                if (!rules.ContainsKey(reverseKey))
                    rules[reverseKey] = reverse;
            }

            Add(UnitRule.Identity(Units.CubicMeter));
            Add(UnitRule.Identity(Units.SquareMeter));
            Add(UnitRule.Identity(Units.Meter));
            Add(UnitRule.Identity(Units.Ton));
            Add(UnitRule.Identity(Units.Kilogram));

            // Thickness/Width/Length inputs are meters.
            Add(UnitRule.TwoWay(
                Units.SquareMeter,
                Units.CubicMeter,
                (Q, p) => Q * p[ParamName.Thickness],
                (Q, p) => Q / p[ParamName.Thickness],
                ParamDef.User(ParamName.Thickness)));

            Add(UnitRule.TwoWay(
                Units.Meter,
                Units.CubicMeter,
                (Q, p) => Q * p[ParamName.Thickness] * p[ParamName.Width],
                (Q, p) => Q / (p[ParamName.Thickness] * p[ParamName.Width]),
                ParamDef.User(ParamName.Thickness),
                ParamDef.User(ParamName.Width)));

            Add(UnitRule.TwoWay(
                Units.Piece,
                Units.CubicMeter,
                (Q, p) => Q * p[ParamName.Thickness] * p[ParamName.Width] * p[ParamName.Length],
                (Q, p) => Q / (p[ParamName.Thickness] * p[ParamName.Width] * p[ParamName.Length]),
                ParamDef.User(ParamName.Thickness),
                ParamDef.User(ParamName.Width),
                ParamDef.User(ParamName.Length)));

            Add(UnitRule.TwoWay(
                Units.Ton,
                Units.CubicMeter,
                (Q, p) => Q / p[ParamName.Density],
                (Q, p) => Q * p[ParamName.Density],
                ParamDef.Resource(ParamName.Density)));

            Add(UnitRule.TwoWay(
                Units.Kilogram,
                Units.CubicMeter,
                (Q, p) => Q / p[ParamName.Density],
                (Q, p) => Q * p[ParamName.Density],
                ParamDef.Resource(ParamName.Density)));

            Add(UnitRule.TwoWay(
                Units.SquareMeter,
                Units.Ton,
                (Q, p) => Q * p[ParamName.Thickness] * p[ParamName.Density],
                (Q, p) => Q / (p[ParamName.Thickness] * p[ParamName.Density]),
                ParamDef.User(ParamName.Thickness),
                ParamDef.Resource(ParamName.Density)));

            Add(UnitRule.TwoWay(
                Units.Meter,
                Units.Ton,
                (Q, p) => Q * p[ParamName.Thickness] * p[ParamName.Width] * p[ParamName.Density],
                (Q, p) => Q / (p[ParamName.Thickness] * p[ParamName.Width] * p[ParamName.Density]),
                ParamDef.User(ParamName.Thickness),
                ParamDef.User(ParamName.Width),
                ParamDef.Resource(ParamName.Density)));

            Add(UnitRule.TwoWay(
                Units.Piece,
                Units.Ton,
                (Q, p) => Q * p[ParamName.Thickness] * p[ParamName.Width] * p[ParamName.Length] * p[ParamName.Density],
                (Q, p) => Q / (p[ParamName.Thickness] * p[ParamName.Width] * p[ParamName.Length] * p[ParamName.Density]),
                ParamDef.User(ParamName.Thickness),
                ParamDef.User(ParamName.Width),
                ParamDef.User(ParamName.Length),
                ParamDef.Resource(ParamName.Density)));

            Add(UnitRule.TwoWay(Units.Kilometer, Units.Meter, (Q, _) => Q * 1000m, (Q, _) => Q / 1000m));
            Add(UnitRule.TwoWay(Units.Centimeter, Units.Meter, (Q, _) => Q / 100m, (Q, _) => Q * 100m));
            Add(UnitRule.TwoWay(Units.Millimeter, Units.Meter, (Q, _) => Q / 1000m, (Q, _) => Q * 1000m));
            Add(UnitRule.TwoWay(Units.Gram, Units.Kilogram, (Q, _) => Q / 1000m, (Q, _) => Q * 1000m));

            return rules;
        }

        public static bool TryGet(string from, string to, out UnitRule rule)
        {
            var fromKey = BuildKey(from);
            var toKey   = BuildKey(to);

            if (Rules.TryGetValue((fromKey, toKey), out var found))
            {
                rule = found;
                return true;
            }
            rule = null!;
            return false;
        }

        public static string BuildKey(string u) => (u ?? "").Trim().ToLowerInvariant() switch
        {
            "meter" or "metre"                    => "m",
            "m1"                                  => "m",
            "kilometer" or "kilometre" or "km."   => "km",
            "centimeter" or "centimetre" or "cm." => "cm",
            "millimeter" or "millimetre"
                or "mm." or "mil"                 => "mm",
            "gram"                                => "g",
            "kilogram"                            => "kg",
            "tonne" or "metric ton"               => "ton",
            _                                     => (u ?? "").Trim().ToLowerInvariant()
        };
    }
}
