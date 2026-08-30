namespace ZARI.Application.Features.Sales.PosClosing.GetAllZReadings;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Sales.PosClosing.RunZReading;
using ZARI.Domain.Common;

/// <summary>History of past Z-Readings for a branch, most recent first.</summary>
public sealed record GetAllZReadingsQuery(string BranchId) : IQuery<Result<List<ZReadingResponse>>>;
