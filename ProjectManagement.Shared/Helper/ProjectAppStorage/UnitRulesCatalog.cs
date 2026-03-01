using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ProjectManagement.Shared.Helper.ProjectAppStorage
{
    file sealed class UnitRuleKeyComparer : IEqualityComparer<(string From, string To)>
    {
        public bool Equals((string From, string To) x, (string From, string To) y) =>
            StringComparer.OrdinalIgnoreCase.Equals(x.From, y.From)
            && StringComparer.OrdinalIgnoreCase.Equals(x.To, y.To);

        public int GetHashCode((string From, string To) obj) =>
            HashCode.Combine(
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.From),
                StringComparer.OrdinalIgnoreCase.GetHashCode(obj.To));
    }

    public static class UnitRulesCatalog
    {
        public static readonly Dictionary<(string From, string To), UnitRule> Rules =
            new(new UnitRuleKeyComparer())
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
                    Compute = (Q, p) => Q * (p[ParamName.Thickness] / 1000.0)
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
                    Compute = (Q, p) => Q * (p[ParamName.Thickness] / 1000.0) * (p[ParamName.Width] / 1000.0)
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
                    * (p[ParamName.Thickness] / 1000.0)
                    * (p[ParamName.Width] / 1000.0)
                    * (p[ParamName.Length] / 1000.0)
                },
                [(Units.Ton, Units.CubicMeter)] = new UnitRule
                {
                    FromUnit = Units.Ton,
                    ToUnit = Units.CubicMeter,
                    Params = { new ParamDef { Key = ParamName.Density, Source = ParamSource.Resource } },
                    Compute = (Q, p) => Q / p[ParamName.Density]
                },
                [(Units.Kilogram, Units.CubicMeter)] = new UnitRule
                {
                    FromUnit = Units.Kilogram,
                    ToUnit = Units.CubicMeter,
                    Params = { new ParamDef { Key = ParamName.Density, Source = ParamSource.Resource } },
                    Compute = (Q, p) => Q / p[ParamName.Density]
                },
                [(Units.CubicMeter, Units.Ton)] = new UnitRule
                {
                    FromUnit = Units.CubicMeter,
                    ToUnit = Units.Ton,
                    Params = { new ParamDef { Key = ParamName.Density, Source = ParamSource.Resource } },
                    Compute = (Q, p) => Q * p[ParamName.Density]
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
                    Compute = (Q, p) => Q * (p[ParamName.Thickness] / 1000.0) * p[ParamName.Density]
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
                    Compute = (Q, p) => Q * (p[ParamName.Thickness] / 1000.0) 
                    * (p[ParamName.Width] / 1000.0) 
                    * p[ParamName.Density]
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
                     * (p[ParamName.Thickness] / 1000.0)
                     * (p[ParamName.Width] / 1000.0)
                     * (p[ParamName.Length] / 1000.0))
                    * p[ParamName.Density]
                },
                //معدلات لا تحتاج الى مدخلات خارجية
                [(Units.Kilometer, Units.Meter)] = new UnitRule { FromUnit = Units.Kilometer, ToUnit = Units.Meter, Compute = (Q, _) => Q * 1000.0 },
                [(Units.Centimeter, Units.Meter)] = new UnitRule { FromUnit = Units.Centimeter, ToUnit = Units.Meter, Compute = (Q, _) => Q / 100.0 },
                [(Units.Millimeter, Units.Meter)] = new UnitRule { FromUnit = Units.Millimeter, ToUnit = Units.Meter, Compute = (Q, _) => Q / 1000.0 },
                [(Units.Gram, Units.Kilogram)] = new UnitRule { FromUnit = Units.Gram, ToUnit = Units.Kilogram, Compute = (Q, _) => Q / 1000.0 },
                [(Units.Kilogram, Units.Kilogram)] = new UnitRule { FromUnit = Units.Kilogram, ToUnit = Units.Kilogram, Compute = (Q, _) => Q },
                [(Units.Ton, Units.Ton)] = new UnitRule { FromUnit = Units.Ton, ToUnit = Units.Ton, Compute = (Q, _) => Q },
                [(Units.Meter, Units.Meter)] = new UnitRule { FromUnit = Units.Meter, ToUnit = Units.Meter, Compute = (Q, _) => Q },
            };

        public static bool TryGet(string from, string to, [NotNullWhen(true)] out UnitRule? rule) =>
            Rules.TryGetValue((from, to), out rule);

        public static string BuildKey(string? u) => (u ?? "").Trim().ToLowerInvariant() switch
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
