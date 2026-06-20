namespace Persistence.Service.Transfer
{
    /// <summary>Outcome of matching a source value against the receiving tenant's local values.</summary>
    public enum MatchStatus
    {
        /// <summary>No source value to match (nothing to do).</summary>
        Empty,
        /// <summary>Exactly one local value matched.</summary>
        Matched,
        /// <summary>A source value existed but no local value matched.</summary>
        NotFound,
        /// <summary>A source value matched more than one local value — needs a manual choice.</summary>
        Ambiguous
    }

    public readonly record struct LookupRow(int Id, string Name);
    public readonly record struct OrgRow(int Id, string Name, string? Number);
    public readonly record struct AccountRow(int Id, string Code, string Name);

    public readonly record struct MatchResult(int? Id, MatchStatus Status)
    {
        public bool IsMatched => Status == MatchStatus.Matched && Id.HasValue;
    }

    /// <summary>
    /// Name-based matching for cross-tenant ATACOST import. Matching is always within a single
    /// field type, trims whitespace and ignores case. New local values are never created here.
    /// </summary>
    public static class AtacostMatcher
    {
        private static string Norm(string? value) => (value ?? string.Empty).Trim();

        private static bool SameName(string a, string b)
            => string.Equals(Norm(a), Norm(b), StringComparison.OrdinalIgnoreCase);

        /// <summary>Match a single dropdown value by name within its own field type.</summary>
        public static MatchResult MatchByName(string? sourceName, IReadOnlyList<LookupRow> candidates)
        {
            if (string.IsNullOrWhiteSpace(sourceName))
                return new MatchResult(null, MatchStatus.Empty);

            var hits = candidates.Where(c => SameName(c.Name, sourceName)).ToList();
            return hits.Count switch
            {
                1 => new MatchResult(hits[0].Id, MatchStatus.Matched),
                0 => new MatchResult(null, MatchStatus.NotFound),
                _ => new MatchResult(null, MatchStatus.Ambiguous)
            };
        }

        /// <summary>
        /// Match an organisation: by organisation number first (when present), then by name.
        /// Multiple name hits are reported as ambiguous (manual choice needed).
        /// </summary>
        public static MatchResult MatchOrganisation(string? sourceNumber, string? sourceName, IReadOnlyList<OrgRow> candidates)
        {
            if (string.IsNullOrWhiteSpace(sourceNumber) && string.IsNullOrWhiteSpace(sourceName))
                return new MatchResult(null, MatchStatus.Empty);

            if (!string.IsNullOrWhiteSpace(sourceNumber))
            {
                var byNumber = candidates
                    .Where(c => !string.IsNullOrWhiteSpace(c.Number) && SameName(c.Number!, sourceNumber))
                    .ToList();
                if (byNumber.Count == 1)
                    return new MatchResult(byNumber[0].Id, MatchStatus.Matched);
                if (byNumber.Count > 1)
                    return new MatchResult(null, MatchStatus.Ambiguous);
            }

            if (string.IsNullOrWhiteSpace(sourceName))
                return new MatchResult(null, MatchStatus.NotFound);

            var byName = candidates.Where(c => SameName(c.Name, sourceName)).ToList();
            return byName.Count switch
            {
                1 => new MatchResult(byName[0].Id, MatchStatus.Matched),
                0 => new MatchResult(null, MatchStatus.NotFound),
                _ => new MatchResult(null, MatchStatus.Ambiguous)
            };
        }

        /// <summary>
        /// Match an account by code AND name together. A code-only match with a differing name
        /// is treated as ambiguous (a warning / manual choice), never silently accepted.
        /// </summary>
        public static MatchResult MatchAccount(string? sourceCode, string? sourceName, IReadOnlyList<AccountRow> candidates)
        {
            if (string.IsNullOrWhiteSpace(sourceCode) && string.IsNullOrWhiteSpace(sourceName))
                return new MatchResult(null, MatchStatus.Empty);

            var exact = candidates
                .Where(c => SameName(c.Code, sourceCode ?? string.Empty) && SameName(c.Name, sourceName ?? string.Empty))
                .ToList();
            if (exact.Count == 1)
                return new MatchResult(exact[0].Id, MatchStatus.Matched);
            if (exact.Count > 1)
                return new MatchResult(null, MatchStatus.Ambiguous);

            // Code matches but name differs → warn, do not accept automatically.
            if (!string.IsNullOrWhiteSpace(sourceCode)
                && candidates.Any(c => SameName(c.Code, sourceCode)))
                return new MatchResult(null, MatchStatus.Ambiguous);

            return new MatchResult(null, MatchStatus.NotFound);
        }
    }
}
