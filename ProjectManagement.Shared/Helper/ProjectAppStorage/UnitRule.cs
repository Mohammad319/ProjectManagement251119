using System;
using System.Collections.Generic;

namespace ProjectManagement.Shared.Helper.ProjectAppStorage
{
    public enum ParamSource { User, Resource }
    public enum ParamName { Thickness, Width, Length, Density }


    public static class Units
    {
        public const string CubicMeter = "m3";
        public const string SquareMeter = "m2";
        public const string Meter = "m";
        public const string Millimeter = "mm";
        public const string Centimeter = "cm";
        public const string Kilometer = "km";
        public const string Ton = "ton";
        public const string Kilogram = "kg";
        public const string Gram = "g";
        public const string Piece = "st";
    }

    public sealed class ParamDef
    {
        public required ParamName Key { get; init; }
        public required ParamSource Source { get; init; }
    }

    public sealed class UnitRule
    {
        public required string FromUnit { get; init; }
        public required string ToUnit { get; init; }
        public List<ParamDef> Params { get; init; } = [];
        public required Func<double, IReadOnlyDictionary<ParamName, double>, double> Compute { get; init; }
    }

}
