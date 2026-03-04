using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectManagement.Shared.Helper.ProjectAppStorage
{
    file static class TupleComparer
    {
        public static IEqualityComparer<(T1, T2)> Create<T1, T2>(
            IEqualityComparer<T1> c1, IEqualityComparer<T2> c2) =>
            new Impl<T1, T2>(c1, c2);

        private sealed class Impl<T1, T2>(IEqualityComparer<T1> c1, IEqualityComparer<T2> c2) : IEqualityComparer<(T1, T2)>
        {
            public bool Equals((T1, T2) x, (T1, T2) y) => c1.Equals(x.Item1, y.Item1) && c2.Equals(x.Item2, y.Item2);
            public int GetHashCode((T1, T2) obj) => System.HashCode.Combine(c1.GetHashCode(obj.Item1!), c2.GetHashCode(obj.Item2!));
        }
    }

    public static class UnitRulesCatalog
    {
        public static readonly Dictionary<(string From, string To), UnitRule> Rules =
            new(TupleComparer.Create<string, string>(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase))
            {
                [(Units.CubicMeter, Units.CubicMeter)] = new UnitRule
                {
                    FromUnit = Units.CubicMeter,
                    ToUnit = Units.CubicMeter,
                    Compute = (Q, _) => Q
                },
                [(Units.SquareMeter, Units.CubicMeter)] = new UnitRule
                {
                    FromUnit = Units.SquareMeter,
                    ToUnit = Units.CubicMeter,
                    Params = { new ParamDef { Key = ParamName.Thickness, Source = ParamSource.User } },
                    Compute = (Q, p) => Q * (Convert.ToDouble(p[ParamName.Thickness]) / 1000d)
                },

                [(Units.Meter, Units.CubicMeter)] = new UnitRule
                {
                    FromUnit = Units.Meter,
                    ToUnit = Units.CubicMeter,
                    Params =
                        {
                        new ParamDef { Key = ParamName.Thickness, Source = ParamSource.User },
                        new ParamDef { Key = ParamName.Width,     Source = ParamSource.User }
                        },
                    Compute = (Q, p) => Q * (Convert.ToDouble(p[ParamName.Thickness]) / 1000d) * (Convert.ToDouble(p[ParamName.Width]) / 1000d)
                },

                [(Units.Piece, Units.CubicMeter)] = new UnitRule
                {
                    FromUnit = Units.Piece,
                    ToUnit = Units.CubicMeter,
                    Params =
                    {
                        new ParamDef { Key = ParamName.Thickness, Source = ParamSource.User },
                        new ParamDef { Key = ParamName.Width,     Source = ParamSource.User },
                        new ParamDef { Key = ParamName.Length,    Source = ParamSource.User }
                    },
                    Compute = (Q, p) => Q
                    * (Convert.ToDouble(p[ParamName.Thickness]) / 1000d)
                    * (Convert.ToDouble(p[ParamName.Width]) / 1000d)
                    * (Convert.ToDouble(p[ParamName.Length]) / 1000d)
                },
                [(Units.Ton, Units.CubicMeter)] = new UnitRule
                {
                    FromUnit = Units.Ton,
                    ToUnit = Units.CubicMeter,
                    Params = { new ParamDef { Key = ParamName.Density, Source = ParamSource.Resource } },
                    Compute = (Q, p) => Q / Convert.ToDouble(p[ParamName.Density])
                },
                [(Units.Kilogram, Units.CubicMeter)] = new UnitRule
                {
                    FromUnit = Units.Kilogram,
                    ToUnit = Units.CubicMeter,
                    Params = { new ParamDef { Key = ParamName.Density, Source = ParamSource.Resource } },
                    Compute = (Q, p) => Q / Convert.ToDouble(p[ParamName.Density])
                },
                [(Units.CubicMeter, Units.Ton)] = new UnitRule
                {
                    FromUnit = Units.CubicMeter,
                    ToUnit = Units.Ton,
                    Params = { new ParamDef { Key = ParamName.Density, Source = ParamSource.Resource } },
                    Compute = (Q, p) => Q * Convert.ToDouble(p[ParamName.Density])
                },
                [(Units.SquareMeter, Units.Ton)] = new UnitRule
                {
                    FromUnit = Units.SquareMeter,
                    ToUnit = Units.Ton,
                    Params =
                    {
                        new ParamDef { Key = ParamName.Thickness, Source = ParamSource.User },
                        new ParamDef { Key = ParamName.Density,   Source = ParamSource.Resource }
                    },
                    Compute = (Q, p) => Q * (Convert.ToDouble(p[ParamName.Thickness]) / 1000d) * Convert.ToDouble(p[ParamName.Density])
                },
                [(Units.Meter, Units.Ton)] = new UnitRule
                {
                    FromUnit = Units.Meter,
                    ToUnit = Units.Ton,
                    Params =
                    {
                        new ParamDef { Key = ParamName.Thickness, Source = ParamSource.User },
                        new ParamDef { Key = ParamName.Width,     Source = ParamSource.User },
                        new ParamDef { Key = ParamName.Density,   Source = ParamSource.Resource }
                    },
                    Compute = (Q, p) => Q * (Convert.ToDouble(p[ParamName.Thickness]) / 1000d) 
                    * (Convert.ToDouble(p[ParamName.Width]) / 1000d) 
                    * Convert.ToDouble(p[ParamName.Density])
                },
                [(Units.Piece, Units.Ton)] = new UnitRule
                {
                    FromUnit = Units.Piece,
                    ToUnit = Units.Ton,
                    Params =
                {
                new ParamDef { Key = ParamName.Thickness, Source = ParamSource.User },
                new ParamDef { Key = ParamName.Width,     Source = ParamSource.User },
                new ParamDef { Key = ParamName.Length,    Source = ParamSource.User },
                new ParamDef { Key = ParamName.Density,   Source = ParamSource.Resource }
                },
                    Compute = (Q, p) =>(Q
                     * (Convert.ToDouble(p[ParamName.Thickness]) / 1000d)
                     * (Convert.ToDouble(p[ParamName.Width]) / 1000d)
                     * (Convert.ToDouble(p[ParamName.Length]) / 1000d))
                    * Convert.ToDouble(p[ParamName.Density])
                },
                //معدلات لا تحتاج الى مدخلات خارجية
                [(Units.Kilometer, Units.Meter)] = new UnitRule { FromUnit = Units.Kilometer, ToUnit = Units.Meter, Compute = (Q, _) => Q * 1000d },
                [(Units.Centimeter, Units.Meter)] = new UnitRule { FromUnit = Units.Centimeter, ToUnit = Units.Meter, Compute = (Q, _) => Q / 100d },
                [(Units.Millimeter, Units.Meter)] = new UnitRule { FromUnit = Units.Millimeter, ToUnit = Units.Meter, Compute = (Q, _) => Q / 1000d },
                [(Units.Gram, Units.Kilogram)] = new UnitRule { FromUnit = Units.Gram, ToUnit = Units.Kilogram, Compute = (Q, _) => Q / 1000d },
                [(Units.Kilogram, Units.Kilogram)] = new UnitRule { FromUnit = Units.Kilogram, ToUnit = Units.Kilogram, Compute = (Q, _) => Q },
                [(Units.Ton, Units.Ton)] = new UnitRule { FromUnit = Units.Ton, ToUnit = Units.Ton, Compute = (Q, _) => Q },
                [(Units.Meter, Units.Meter)] = new UnitRule { FromUnit = Units.Meter, ToUnit = Units.Meter, Compute = (Q, _) => Q },
            };

        public static bool TryGet(string from, string to, out UnitRule rule)
        {
            if (Rules.TryGetValue((from, to), out var foundRule))
            {
                rule = foundRule;
                return true;
            }

            rule = null!;
            return false;
        }

        public static string BuildKey(string u) => (u ?? "").Trim().ToLowerInvariant() switch
        {

            "meter" or "metre" => "m",
            "m1" => "m",
            "kilometer" or "kilometre" or "km." => "km",
            "centimeter" or "centimetre" or "cm." => "cm",
            "millimeter" or "millimetre" or "mm." or "mil" => "mm", // mil = mm هنا
            "gram" => "g",
            "kilogram" => "kg",
            "tonne" or "metric ton" => "ton",
            _ => (u ?? "").Trim().ToLowerInvariant()
        };
    }

}
