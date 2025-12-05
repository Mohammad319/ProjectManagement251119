//namespace TaskResourceBlueprints.Helper
//{
//    file static class TupleComparer
//    {
//        public static IEqualityComparer<(T1, T2)> Create<T1, T2>(
//            IEqualityComparer<T1> c1, IEqualityComparer<T2> c2) =>
//            new Impl<T1, T2>(c1, c2);

//        private sealed class Impl<T1, T2> : IEqualityComparer<(T1, T2)>
//        {
//            private readonly IEqualityComparer<T1> _c1;
//            private readonly IEqualityComparer<T2> _c2;
//            public Impl(IEqualityComparer<T1> c1, IEqualityComparer<T2> c2) { _c1 = c1; _c2 = c2; }
//            public bool Equals((T1, T2) x, (T1, T2) y) => _c1.Equals(x.Item1, y.Item1) && _c2.Equals(x.Item2, y.Item2);
//            public int GetHashCode((T1, T2) obj) => System.HashCode.Combine(_c1.GetHashCode(obj.Item1), _c2.GetHashCode(obj.Item2));
//        }
//    }

//    public static class UnitRulesCatalog
//    {
//        public static readonly Dictionary<(string From, string To), UnitRule> Rules =
//            new(TupleComparer.Create(StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase))
//            {
//                // === إلى m3 ===
//                [(Units.CubicMeter, Units.CubicMeter)] = new UnitRule
//                {
//                    FromUnit = Units.CubicMeter,
//                    ToUnit = Units.CubicMeter,
//                    Compute = (Q, _) => Q
//                },

//                // m2 -> m3   Q * (Thickness/1000)
//                [(Units.SquareMeter, Units.CubicMeter)] = new UnitRule
//                {
//                    FromUnit = Units.SquareMeter,
//                    ToUnit = Units.CubicMeter,
//                    Params = { new ParamDef { Key = ParamName.Thickness, Source = ParamSource.User } },
//                    Compute = (Q, p) => Q * (p[ParamName.Thickness] / 1000.0)
//                },

//                // m -> m3    Q * (t/1000) * (w/1000)
//                [(Units.Meter, Units.CubicMeter)] = new UnitRule
//                {
//                    FromUnit = Units.Meter,
//                    ToUnit = Units.CubicMeter,
//                    Params =
//                {
//                new ParamDef { Key = ParamName.Thickness, Source = ParamSource.User },
//                new ParamDef { Key = ParamName.Width,     Source = ParamSource.User }
//                },
//                    Compute = (Q, p) => Q * (p[ParamName.Thickness] / 1000.0) * (p[ParamName.Width] / 1000.0)
//                },

//                // st -> m3   Q * (t/1000) * (w/1000) * (l/1000)
//                [(Units.Piece, Units.CubicMeter)] = new UnitRule
//                {
//                    FromUnit = Units.Piece,
//                    ToUnit = Units.CubicMeter,
//                    Params =
//                {
//                new ParamDef { Key = ParamName.Thickness, Source = ParamSource.User },
//                new ParamDef { Key = ParamName.Width,     Source = ParamSource.User },
//                new ParamDef { Key = ParamName.Length,    Source = ParamSource.User }
//                },
//                    Compute = (Q, p) => Q
//                    * (p[ParamName.Thickness] / 1000.0)
//                    * (p[ParamName.Width] / 1000.0)
//                    * (p[ParamName.Length] / 1000.0)
//                },

//                // ton -> m3  Q / Density
//                [(Units.Ton, Units.CubicMeter)] = new UnitRule
//                {
//                    FromUnit = Units.Ton,
//                    ToUnit = Units.CubicMeter,
//                    Params = { new ParamDef { Key = ParamName.Density, Source = ParamSource.Resource } }, // ton/m3
//                    Compute = (Q, p) => Q / p[ParamName.Density]
//                },

//                // kg -> m3   (kg)/(ton/m3) = m3
//                [(Units.Kilogram, Units.CubicMeter)] = new UnitRule
//                {
//                    FromUnit = Units.Kilogram,
//                    ToUnit = Units.CubicMeter,
//                    Params = { new ParamDef { Key = ParamName.Density, Source = ParamSource.Resource } },
//                    Compute = (Q, p) => Q / p[ParamName.Density]
//                },

//                // === إلى ton ===
//                // m3 -> ton   Q * Density
//                [(Units.CubicMeter, Units.Ton)] = new UnitRule
//                {
//                    FromUnit = Units.CubicMeter,
//                    ToUnit = Units.Ton,
//                    Params = { new ParamDef { Key = ParamName.Density, Source = ParamSource.Resource } },
//                    Compute = (Q, p) => Q * p[ParamName.Density]
//                },

//                // m2 -> ton   Q*(t/1000)*Density
//                [(Units.SquareMeter, Units.Ton)] = new UnitRule
//                {
//                    FromUnit = Units.SquareMeter,
//                    ToUnit = Units.Ton,
//                    Params =
//                {
//                new ParamDef { Key = ParamName.Thickness, Source = ParamSource.User },
//                new ParamDef { Key = ParamName.Density,   Source = ParamSource.Resource }
//                },
//                    Compute = (Q, p) => Q * (p[ParamName.Thickness] / 1000.0) * p[ParamName.Density]
//                },

//                // m  -> ton   Q*(t/1000)*(w/1000)*Density
//                [(Units.Meter, Units.Ton)] = new UnitRule
//                {
//                    FromUnit = Units.Meter,
//                    ToUnit = Units.Ton,
//                    Params =
//                {
//                new ParamDef { Key = ParamName.Thickness, Source = ParamSource.User },
//                new ParamDef { Key = ParamName.Width,     Source = ParamSource.User },
//                new ParamDef { Key = ParamName.Density,   Source = ParamSource.Resource }
//                },
//                    Compute = (Q, p) => Q
//                    * (p[ParamName.Thickness] / 1000.0)
//                    * (p[ParamName.Width] / 1000.0)
//                    * p[ParamName.Density]
//                },

//                // st -> ton   (st->m3)*Density
//                [(Units.Piece, Units.Ton)] = new UnitRule
//                {
//                    FromUnit = Units.Piece,
//                    ToUnit = Units.Ton,
//                    Params =
//                {
//                new ParamDef { Key = ParamName.Thickness, Source = ParamSource.User },
//                new ParamDef { Key = ParamName.Width,     Source = ParamSource.User },
//                new ParamDef { Key = ParamName.Length,    Source = ParamSource.User },
//                new ParamDef { Key = ParamName.Density,   Source = ParamSource.Resource }
//                },
//                    Compute = (Q, p) =>
//                    (Q
//                     * (p[ParamName.Thickness] / 1000.0)
//                     * (p[ParamName.Width] / 1000.0)
//                     * (p[ParamName.Length] / 1000.0))
//                    * p[ParamName.Density]
//                },

//                // === إلى m (تحويلات طول مباشرة) ===
//                [(Units.Kilometer, Units.Meter)] = new UnitRule { FromUnit = Units.Kilometer, ToUnit = Units.Meter, Compute = (Q, _) => Q * 1000.0 },
//                [(Units.Centimeter, Units.Meter)] = new UnitRule { FromUnit = Units.Centimeter, ToUnit = Units.Meter, Compute = (Q, _) => Q / 100.0 },
//                [(Units.Millimeter, Units.Meter)] = new UnitRule { FromUnit = Units.Millimeter, ToUnit = Units.Meter, Compute = (Q, _) => Q / 1000.0 },

//                // === كتل وهويات ===
//                [(Units.Gram, Units.Kilogram)] = new UnitRule { FromUnit = Units.Gram, ToUnit = Units.Kilogram, Compute = (Q, _) => Q / 1000.0 },
//                [(Units.Kilogram, Units.Kilogram)] = new UnitRule { FromUnit = Units.Kilogram, ToUnit = Units.Kilogram, Compute = (Q, _) => Q },
//                [(Units.Ton, Units.Ton)] = new UnitRule { FromUnit = Units.Ton, ToUnit = Units.Ton, Compute = (Q, _) => Q },
//                [(Units.Meter, Units.Meter)] = new UnitRule { FromUnit = Units.Meter, ToUnit = Units.Meter, Compute = (Q, _) => Q },
//            };

//        public static bool TryGet(string from, string to, out UnitRule rule) =>
//            Rules.TryGetValue((from, to), out rule);

//        public static bool Requires(this UnitRule rule, ParamName name) =>
//            rule.Params.Any(p => p.Key == name);

//        // تنفيذ آمن مع تحقق من الباراميترات المطلوبة
//        public static double ComputeOrThrow(string from, string to, double quantity,
//            IReadOnlyDictionary<ParamName, double> args)
//        {
//            if (!TryGet(from, to, out var rule))
//                throw new KeyNotFoundException($"No rule found for '{from}->{to}'.");

//            // تحقق من كل باراميتر مطلوب
//            var missing = rule.Params.Select(p => p.Key).Where(k => !args.ContainsKey(k)).ToList();
//            if (missing.Count > 0)
//                throw new ArgumentException($"Missing parameters: {string.Join(", ", missing)}");

//            return rule.Compute(quantity, args);
//        }

//        // طبّع أسماء الوحدات إلى صيغة موحّدة (lowercase + مرادفات)
//        public static string Normalize(string u) => (u ?? "").Trim().ToLowerInvariant() switch
//        {

//            "meter" or "metre" => "m",
//            "m1" => "m",   // لو عندك m1 وتريد اعتباره m
//            "kilometer" or "kilometre" or "km." => "km",
//            "centimeter" or "centimetre" or "cm." => "cm",
//            "millimeter" or "millimetre" or "mm." or "mil" => "mm", // mil = mm هنا
//            "gram" => "g",
//            "kilogram" => "kg",
//            "tonne" or "metric ton" => "ton",
//            _ => (u ?? "").Trim().ToLowerInvariant()
//        };

//        public static string BuildKey(string from, string to)
//            => $"{Normalize(from)}";
//        //=> $"{Normalize(from)}-{Normalize(to)}";
//    }

//}
