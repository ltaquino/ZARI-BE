namespace ZARI.Application.Features.Sales.StatutoryDiscountTypes.Delete;

using ZARI.Application.Abstractions.Messaging;

public sealed record DeleteStatutoryDiscountTypeCommand(Guid Id) : ICommand;
