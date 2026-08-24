using ZARI.Application.Abstractions.Messaging;
using ZARI.Domain.Common;

namespace ZARI.Application.Features.Identity.Register;

public sealed record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string ConfirmPassword,
    string Roles) : ICommand;
