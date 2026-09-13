namespace ZARI.Application.Features.Hr.LeaveRequests.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteLeaveRequestCommand(Guid Id) : ICommand;
