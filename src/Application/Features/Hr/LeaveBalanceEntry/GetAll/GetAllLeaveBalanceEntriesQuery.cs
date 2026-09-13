namespace ZARI.Application.Features.Hr.LeaveBalanceEntries.GetAll;

using ZARI.Application.Abstractions.Messaging;
using ZARI.Application.Features.Hr.LeaveBalanceEntries.Get;
using ZARI.Domain.Common;

public sealed record GetAllLeaveBalanceEntriesQuery : IQuery<Result<List<LeaveBalanceEntryResponse>>>;
