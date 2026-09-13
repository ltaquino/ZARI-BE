using System.Runtime.CompilerServices;

// Lets the unit test project reach `internal static class` shared logic (calculators, generators,
// eligibility rules) directly — that's exactly where this session found real bugs live-testing
// (the CDA report's LINQ translation failure, the restructured-loan classification edge case), so
// it's the highest-value surface for unit coverage, not something to wall off from tests.
[assembly: InternalsVisibleTo("ZARI.Application.UnitTests")]
