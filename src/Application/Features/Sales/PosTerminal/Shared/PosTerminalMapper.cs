namespace ZARI.Application.Features.Sales.PosTerminals.Shared;

using ZARI.Application.Features.Sales.PosTerminals.Get;
using ZARI.Domain.Entities;

internal static class PosTerminalMapper
{
    public static PosTerminalResponse ToResponse(PosTerminal t) => new(
        t.Id, t.BranchId, t.Code, t.Name, t.MachineIdentificationNumber, t.MachineSerialNumber, t.PosPermitNumber,
        t.PosPermitDateIssued, t.Status, t.CreatedAt, t.HasOwnSeries, t.OrPrefix, t.OrNextNumber, t.OrPaddingLength, t.ZCounter);
}
