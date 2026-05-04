using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectManagement.Shared.Helper.ProjectAppStorage
{
    public enum ParamSource { User, Resource }
    public enum ParamName { Thickness, Width, Length, Density }

    public static class Units
    {
        public const string CubicMeter  = "m3";
        public const string SquareMeter = "m2";
        public const string Meter       = "m";
        public const string Millimeter  = "mm";
        public const string Centimeter  = "cm";
        public const string Kilometer   = "km";
        public const string Ton         = "ton";
        public const string Kilogram    = "kg";
        public const string Gram        = "g";
        public const string Piece       = "st";
    }

    public sealed class ParamDef
    {
        public required ParamName   Key    { get; init; }
        public required ParamSource Source { get; init; }

        public static ParamDef User(ParamName key) => new()
        {
            Key = key,
            Source = ParamSource.User,
        };

        public static ParamDef Resource(ParamName key) => new()
        {
            Key = key,
            Source = ParamSource.Resource,
        };
    }

    public sealed class UnitRule
    {
        public required string     FromUnit { get; init; }
        public required string     ToUnit   { get; init; }
        public List<ParamDef>      Params   { get; init; } = [];

        public required Func<decimal, IReadOnlyDictionary<ParamName, decimal>, decimal> Compute { get; init; }

        public static UnitRule Identity(string unit) => new()
        {
            FromUnit = unit,
            ToUnit = unit,
            Compute = (Q, _) => Q,
        };

        public static UnitRule OneWay(
            string fromUnit,
            string toUnit,
            Func<decimal, IReadOnlyDictionary<ParamName, decimal>, decimal> compute,
            params ParamDef[] parameters) => new()
            {
                FromUnit = fromUnit,
                ToUnit = toUnit,
                Params = parameters.ToList(),
                Compute = compute,
            };

        public static UnitRule TwoWay(
            string fromUnit,
            string toUnit,
            Func<decimal, IReadOnlyDictionary<ParamName, decimal>, decimal> compute,
            Func<decimal, IReadOnlyDictionary<ParamName, decimal>, decimal> reverseCompute,
            params ParamDef[] parameters) => new()
            {
                FromUnit = fromUnit,
                ToUnit = toUnit,
                Params = parameters.ToList(),
                Compute = compute,
                ReverseCompute = reverseCompute,
            };

        /// <summary>
        /// Inverse formula (From ↔ To). Null means this direction has no automatic inverse.
        /// </summary>
        public Func<decimal, IReadOnlyDictionary<ParamName, decimal>, decimal>? ReverseCompute { get; init; }

        /// <summary>
        /// Returns a new UnitRule with From/To swapped and Compute ↔ ReverseCompute.
        /// Returns null when no ReverseCompute is defined.
        /// </summary>
        public UnitRule? Reverse()
        {
            if (ReverseCompute is null) return null;
            return new UnitRule
            {
                FromUnit       = ToUnit,
                ToUnit         = FromUnit,
                Params         = Params.ToList(),
                Compute        = ReverseCompute,
                ReverseCompute = Compute,
            };
        }
    }
}
