namespace ZARI.Application.Features.Sales.PosTerminals.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosTerminals.Get;
using ZARI.Domain.Common;

/// <summary>Optional BranchId narrows to one branch's terminals (the POS setup screen's own Terminal dropdown) — omitted, returns every branch's, same "no per-row filtering" convention as every other GetAll in this codebase (View permission on that specific branch is still checked per row).</summary>
public sealed record GetAllPosTerminalsQuery(string? BranchId) : IQuery<Result<List<PosTerminalResponse>>>;
