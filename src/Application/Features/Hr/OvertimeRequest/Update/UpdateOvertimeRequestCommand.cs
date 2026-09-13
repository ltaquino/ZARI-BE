namespace ZARI.Application.Features.Hr.OvertimeRequests.Update;

using ZARI.Application.Abstractions.Messaging;

public sealed record UpdateOvertimeRequestCommand(
    Guid Id,
    DateTimeOffset Date,
    DateTimeOffset PlannedTimeFrom,
    DateTimeOffset PlannedTimeTo,
    string OtType,
    string? Reason) : ICommand;
