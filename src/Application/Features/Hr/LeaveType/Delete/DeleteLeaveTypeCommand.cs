namespace ZARI.Application.Features.Hr.LeaveTypes.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteLeaveTypeCommand(Guid Id) : ICommand;
